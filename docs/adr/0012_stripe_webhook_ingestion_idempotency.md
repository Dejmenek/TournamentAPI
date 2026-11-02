# Title
Stripe Webhook Ingestion and Idempotency

# Date
02/11/2026

## Status
Accepted

## Context
Paid tournaments (#172) learn about payments, refunds, account changes and disputes from Stripe
webhooks. Stripe delivers events at least once: the same event can arrive more than once, events
can arrive out of order, and a delivery that gets no 2xx response is retried for up to three days.
A handler that runs twice for the same `checkout.session.completed` could confirm a seat twice or
issue a second refund.

The endpoint also has to be reachable by Stripe without a user or API key, and nobody may be able
to fake an event. Stripe can send many deliveries in a short burst from a small set of IP
addresses, and ADR #0001's per-IP token bucket (100 tokens, refilling 50 per minute) would throttle
them.

## Considered Options

### For deduplication
1. A `ProcessedStripeEvents` row keyed by `event.id`, inserted in the same SQL Server transaction as
   the handler's change (chosen)
   - Pros: The primary key decides which delivery wins, the same unique-index arbitration ADR #0004
     uses for slots. The row and the domain change commit or roll back together, so a failed
     handler leaves no row behind and Stripe's retry runs it again. No new infrastructure.
   - Cons: One extra insert per handled event, and a table that grows without a cleanup job.
     Webhook throughput is tied to the main database.
2. Deduplication in Redis (`SET NX` on the event ID)
   - Pros: Fast, and the key can expire on its own.
   - Cons: A new piece of infrastructure, and the key can't share a transaction with the SQL
     change. If the handler fails after the key is set, the retry is wrongly skipped. If the key is
     set after the commit, a crash between the two runs the handler twice.
3. No event log, relying only on state checks in each handler
   - Pros: Nothing extra to store.
   - Cons: Every handler has to be perfectly idempotent on its own, including the ones that call
     Stripe. One missed check turns a duplicate delivery into a duplicate refund or transfer.

### For rate limiting the endpoint
1. Exempt `/webhooks/stripe` from the per-IP token bucket and keep it under the global concurrency
   limiter (chosen)
   - Pros: Stripe's bursts aren't rejected with 429, and the server is still protected from too
     many requests running at once. Requests without a valid signature are rejected before any
     database work.
   - Cons: An attacker can send unlimited unsigned requests to this path. Each one costs a signature
     check (an HMAC over the body) and still takes a concurrency permit while it runs.
2. Keep the endpoint under both limiters
   - Pros: No change to ADR #0001.
   - Cons: Bursts of legitimate deliveries get 429s. Stripe retries them, but with backoff, which
     delays payment confirmations.
3. A separate, larger token bucket for the webhook path
   - Pros: Still caps abuse of the path.
   - Cons: We have no delivery rate to size it against, so the limit would be a guess, and a
     wrong guess brings back the 429s.

## Decision
`POST /webhooks/stripe` is a minimal API endpoint outside GraphQL with no authentication. It
verifies the `Stripe-Signature` header with `EventUtility.ConstructEvent` and returns 400 when the
header is missing or the signature doesn't match. It passes `throwOnApiVersionMismatch: false`, so a
correctly signed event from an endpoint on a different Stripe API version is still accepted instead
of being rejected like a forged one and retried for days. A verified event is dispatched by type to an
`IStripeWebhookHandler`. Event types without a handler are acknowledged with 200 and not recorded.

For a handled type, the endpoint opens a transaction and inserts the `ProcessedStripeEvents` row
before the handler runs. A duplicate primary key `DbUpdateException` means the event was already
handled, so the handler is skipped and the endpoint returns 200. A concurrent duplicate waits on
the first delivery's key lock and then sees either the committed row or, if the first delivery
rolled back, an empty slot. Otherwise the handler makes its changes in the same context, and the
row and the changes commit together. Any other failure rolls back and returns 500 so Stripe
retries.

Every handler still checks the current state before changing it (for example, only
`PendingPayment` can become `Confirmed`), so a replayed or out-of-order event does nothing. Today
the event row is the first line of defence and these checks are the second. If deduplication moves
to Redis later, the checks become the main protection, so they have to be strict from the start.

This amends ADR #0001: the chained global limiter still runs before routing for every request, but
the per-IP token bucket gives `/webhooks/stripe` a no-limit partition. The concurrency limiter
counts webhook requests like any other.

## Related ADRs
- ADR #0001 Rate Limiting Algorithm for API Requests: amended, the webhook path is exempt from the
  per-IP token bucket but stays under the global concurrency limiter.
- ADR #0004 Concurrency Control for Tournament MaxParticipants: reuses the same unique-key
  arbitration with `DbUpdateException` as the signal that another writer got there first.
