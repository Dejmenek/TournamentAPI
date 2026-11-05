# Title
Prize Pool Funding Through Stripe Checkout Sessions

# Date
05/11/2026

## Status
Accepted

## Context
An owner pays the prize pool escrow (prize total plus the platform fee) through Stripe Checkout
before a tournament with prizes can open (ADR #0010, issue #176). Three things can go wrong.
Two simultaneous `fundTournamentPrizePool` calls can create two paid sessions. An owner can
create a session, raise the prizes with `updateTournament`, and then pay the old amount. And
webhooks can be late, lost or arrive for sessions the tournament no longer points at, which
either strands an open session or leaves a paid session that matches nothing.

## Considered Options

### For creating one session per tournament
1. A Stripe idempotency key built from the tournament ID and an attempt counter, plus a
   conditional `ExecuteUpdate` that only fills an empty `FundingSessionId` (chosen)
   - Pros: Same pattern as ADR #0014. Two overlapping calls share a key, so Stripe hands both
     the same session, and the loser of the database update re-reads the winner's session. No
     lock is held across the Stripe call. The update can also require the entry fee and prize
     total to still match what the session was priced from.
   - Cons: A fixed key replays Stripe's first answer, errors included, so the counter has to
     move after a Stripe error and after a session is cleared. An orphaned session can exist
     when the update changes no row. It expires on its own and is never paid.
2. Lock the tournament row while calling Stripe
   - Pros: Only one call reaches Stripe.
   - Cons: Holds a database lock across an external HTTP call.

### For blocking term edits while a session is open
1. `updateTournament` runs a no-op conditional update that only matches while
   `FundingSessionId` is empty, in the same transaction as its `SaveChanges` (chosen)
   - Pros: The edit and the session fill contend on the same row. Whichever commits second sees
     the other's result: the fill's extra check on entry fee and prize total fails after an
     edit, and the edit's guard fails after a fill. No new locking primitive.
   - Cons: The edit now opens a transaction when terms change. The owner waits out the
     Checkout expiry (30 minutes by default) to edit again. A `cancelPrizePoolFunding`
     mutation can lift that later.
2. Check `FundingSessionId` in memory before saving
   - Pros: Simplest.
   - Cons: A session created between the read and the save slips through, which is the
     exploit this decision exists to stop.
3. A `[Timestamp]` row version on `Tournament`
   - Pros: Generic optimistic concurrency.
   - Cons: Every unrelated edit would also conflict with session creation.

### For recognising funding sessions in webhooks
1. One handler per event type, `checkout.session.completed` and `checkout.session.expired`,
   which act only on sessions whose `type` metadata is `prize_pool_funding` (chosen)
   - Pros: ADR #0012 allows one handler per event type, and the entry-payment slice can extend
     the same handlers. The metadata is written by this platform when it creates the session.
   - Cons: Every checkout event now passes through these handlers, even ones they ignore.
2. Look the session ID up in the database and infer the type from the match
   - Pros: No reliance on metadata.
   - Cons: A paid session that matches nothing, which is the case that must be logged, can't be
     told apart from an entry session.

## Decision
`fundTournamentPrizePool` is owner only. It requires a `Closed` tournament that hasn't started
and an `Unfunded` pool, and for a tournament with an entry fee, an `Enabled` payout account.
Prizes-only tournaments don't need a payout account because the owner receives nothing from
them. The session amount is `prize total + ceil(prize total × pct)`, card payments only, with
`transfer_group = tournament-{id}` and `type`, tournament and owner IDs in metadata.

A repeat call reads the stored session through the gateway. An open session returns its URL. An
expired one is cleared (which also moves the attempt counter) and replaced. A `complete` session
is not replaced, because creating a second one could take a second payment. The idempotency key
is `prize-pool-tournament-{id}-{attempt}`, and the counter also moves after a Stripe error other
than 409.

`completed` for a stored, unfunded session reads `latest_charge` through the gateway, then sets
`Funded` and stores the charge ID in one conditional update. `expired` clears the stored session.
A `completed` that matches no stored session, or a replay under a new event ID, changes nothing
and logs at error level with the session ID, because the owner has paid and nothing refunds it
yet. Refunds need `PaymentLedger`, which arrives with paid entries.

We accept that a Stripe call happens inside the webhook transaction (the charge lookup), the
same trade-off as ADR #0014, and that an owner may wait out the Checkout expiry before editing
terms.

## Related ADRs
- ADR #0010 Payments Money Flow for Paid Tournaments: defines the escrow this funds and the
  `transfer_group` and charge the later prize transfers use.
- ADR #0012 Stripe Webhook Ingestion and Idempotency: handlers must check current state, which
  is why `completed` and `expired` use conditional updates.
- ADR #0014 Payout Account Onboarding and Status Ordering: reuses the idempotency key with an
  attempt counter and the conditional fill.
- ADR #0004 Concurrency Control for Tournament MaxParticipants: same idea of letting a
  conditional write arbitrate instead of a lock.
