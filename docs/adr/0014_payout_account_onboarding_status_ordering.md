# Title
Payout Account Onboarding and Status Ordering

# Date
02/11/2026

## Status
Accepted

## Context
Owners and winners need a Stripe connected account before the platform can send them transfers
(ADR #0010). `startStripeOnboarding` (#174) creates the account on first use and returns a
Stripe-hosted Account Link. The user's `PayoutAccountStatus` (`NotStarted`, `Pending`, `Enabled`,
`Restricted`) then follows `account.updated` webhooks.

Three things can go wrong. Two concurrent calls, or a retry after a timeout, can create two
connected accounts for the same user. ADR #0012 removes duplicate deliveries of the same event, but
two different `account.updated` events can still arrive out of order, so an older "pending"
snapshot could overwrite a newer "enabled" one. And Stripe can send `account.updated` for a new
account before the mutation has saved the account ID, so the handler finds no user and the event
is recorded as processed and lost.

## Considered Options

### For ordering account.updated events
1. Compare `event.created` with the stored `PayoutAccountStatusChangedAt` in a conditional update,
   and re-fetch the account from Stripe only when the two are equal (chosen)
   - Pros: Every event carries the full account snapshot, so normally the newest event wins
     without a Stripe call. The comparison is part of the `UPDATE`, so two deliveries for the same
     account processed at the same time can't let an older snapshot overwrite a newer one: the
     second update re-checks the row once it holds the lock and changes nothing. `created` has
     one-second precision, and Stripe often sends several `account.updated` events within a second
     when onboarding finishes. On a tie, the handler maps the live account instead of guessing
     which snapshot is newer.
   - Cons: A Stripe API call inside the webhook transaction on a tie, so a Stripe outage at that
     moment fails the delivery with a 500 and Stripe retries it later. Tests need the gateway fake
     for the tie path.
2. Re-fetch the account from Stripe on every event and ignore the payload
   - Pros: Always uses the current state, so ordering stops mattering.
   - Cons: A Stripe API call while the webhook transaction is open, which ties webhook latency and
     failures to Stripe's API. Tests need the gateway fake for every event.
3. Rank statuses and only allow forward moves
   - Pros: No timestamps needed.
   - Cons: The statuses aren't monotonic. `Enabled` to `Restricted` is a real transition when
     Stripe asks for more verification, and a ranking would block it.

### For preventing duplicate connected accounts
1. A per-user Stripe idempotency key with an attempt counter
   (`connected-account-user-{id}-{attempt}`) plus a filtered unique index on `StripeAccountId`
   (chosen)
   - Pros: A retry within Stripe's idempotency window gets the same account back. Two overlapping
     calls share a key, so Stripe answers the second with a 409 while the first is still running.
     Stripe.net retries 409s, so the second caller most likely gets the same account too. The
     mutation stores the ID with a conditional update that only fills an empty `StripeAccountId`.
     The index stops one account ID from being linked to two users. No locking.
   - Cons: On API v1, Stripe saves the first response for a key, errors included, and replays it
     for at least 24 hours. With a fixed key, one 500 or one rejected parameter (a different
     country, say) would mean a day of failed retries, so the counter moves to a new key after
     Stripe answers with an error. It stays put on a 409, which means the first request is still
     running, and on a connection failure or timeout, where Stripe may already have created the
     account. A 500 can still hide an account that Stripe did create, so moving the counter after
     one can leave an orphaned account. The `user_id` metadata fallback links that account if its
     `account.updated` arrives while the user has no account ID. Otherwise it stays unlinked.
2. A time bucket in the key, for example the current hour
   - Pros: No extra column.
   - Cons: A retry that crosses a bucket boundary creates a second account, even after a plain
     network drop. An error still blocks retries until the bucket ends.
3. Lock the user row (or use a pending-onboarding flag) before calling Stripe
   - Pros: Only one call ever reaches Stripe.
   - Cons: Holds a database lock across an external HTTP call, and still needs cleanup when the
     call fails after the lock was taken.

### For account.updated arriving before the account ID is saved
1. Fall back to the `user_id` metadata written at account creation, only for a user with no
   account ID yet (chosen)
   - Pros: The early event isn't lost. A user who already has a different account can't have it
     replaced.
   - Cons: Trusts metadata the platform wrote itself. It's set on every account we create, but
     accounts made outside this flow won't have it.
2. Ignore unknown accounts and rely on later events
   - Pros: Simplest handler.
   - Cons: The status stays `Pending` until Stripe sends another event, which may not happen until
     the user changes something.

## Decision
When the user has no account yet, `startStripeOnboarding` validates the country against the
EU/EEA list before calling Stripe, so an unsupported country creates nothing. Once an account
exists, the call ignores the country and just returns a fresh link, since the account's country
can't change. On first use it creates an account with Express-style
controller properties (`stripe_dashboard.type = express`, Stripe collects requirements, the
application pays fees and covers losses, `transfers` requested). The idempotency key combines the
user ID with the user's `StripeAccountCreationAttempt`. When Stripe answers with an error other
than a 409, a conditional update moves the counter on, so the next call uses a fresh key.

After a successful create, the mutation stores the ID and sets the status to `Pending` in one
update that only applies while the user has no account ID, so an `account.updated` that was
matched through metadata in the meantime keeps its newer status. If that update changes no row,
another account was linked first. The mutation then re-reads the stored ID, logs a warning when it
differs from the account it just created, and builds the link for the stored account, because
that's the one webhooks will match. Every call returns a fresh Account Link for the stored account.

The `account.updated` handler finds the user by account ID, or by `user_id` metadata when no user
has that ID yet. It writes the mapped status with an `ExecuteUpdate` that only applies while the
stored `PayoutAccountStatusChangedAt` is older than the event, and only while the user has no
account ID or has this one. When the stored time equals the event's, either on the first read or
after an update that changed no row, it fetches the account from Stripe and writes the live state
instead. If that fetch fails, the delivery returns 500 and Stripe retries it.

The mapper reads the `transfers` capability, `details_submitted` and the `requirements` hash. An
active `transfers` capability is `Enabled`, even when more information is due before a deadline,
because the account can still receive transfers. Unsubmitted details, a pending capability, or an
inactive one where Stripe is only verifying what was submitted (`pending_verification` set,
nothing `currently_due` or `past_due`) is `Pending`. Anything else after onboarding is
`Restricted`.

The mutation reports failures instead
of throwing them. Stripe errors, connection failures and timeouts that aren't client cancellations
become `Payment.StripeRequestFailed`. A database failure while saving the account ID becomes
`Payment.PayoutAccountSaveFailed`.

`payoutAccountStatus` is resolved only for the user who owns it, the same rule `email` uses for
private addresses. The account ID and the change timestamp are not in the schema.

We accept the 24-hour idempotency window, which leaves a state the next onboarding call repairs and
can't move money. We also accept the orphaned account a 500 can leave behind. No user points at it,
so the platform never sends it a transfer.

`Enabled` doesn't warn the user when Stripe needs more information before a deadline. When the
deadline passes, Stripe disables transfers and sends `account.updated`, so the status becomes
`Restricted`. Warning earlier needs a new status value or a separate field, which waits until a
client can show it.

## Related ADRs
- ADR #0010 Payments Money Flow for Paid Tournaments: limits connected accounts to the EU/EEA and
  needs them to receive transfers, which is why onboarding exists.
- ADR #0012 Stripe Webhook Ingestion and Idempotency: removes duplicate deliveries. This ADR adds
  the state check for out-of-order events that ADR #0012 asks every handler to make.
- ADR #0004 Concurrency Control for Tournament MaxParticipants: reuses the unique-index
  arbitration, here on `StripeAccountId`.
