# Title
Paid Entry Lifecycle for Tournaments

# Date
04/11/2026

## Status
Accepted

## Context
ADR #0010 settled how money moves for paid tournaments. This ADR covers the lifecycle around it:
where the "is the prize money in?" state lives, how a paying entrant holds a seat while Stripe
Checkout is open, what happens when a payment arrives too late, which terms can still change,
and who pays when a paid tournament is cancelled (#175).

A paying entrant takes minutes to finish Checkout, and the seat count is arbitrated by the unique
`(TournamentId, SlotNumber)` index (ADR #0004). Stripe also delivers webhooks at least once and
in any order (ADR #0012), so a payment can land after the reservation behind it is gone.

## Considered Options

### For the prize pool state
1. A separate `PrizePoolStatus` next to `TournamentStatus` (chosen)
   - Pros: `TournamentStatus` (`Closed`, `Open`, `Completed`) keeps its meaning and all the code
     that reads it keeps working. Funding is tracked on its own (`NotRequired`, `Unfunded`,
     `Funded`, `PaidOut`, `Refunded`), and the only coupling is one rule: a tournament can't be
     set to `Open` unless the pool is `NotRequired` or `Funded`.
   - Cons: Two status fields describe one tournament, and the legal combinations are enforced in
     validation instead of by the type.
2. New `TournamentStatus` values such as `AwaitingFunding`
   - Pros: One field, one lifecycle.
   - Cons: Every existing check on `Open`, `Closed` and `Completed` (auto-close, reopening,
     deletion, bracket generation) would need to learn the new values. Free tournaments would
     carry states that never apply to them.

### For holding a seat during payment
1. Seat reservation with `EntryStatus` (chosen)
   - Pros: A paying entrant gets a `TournamentParticipant` row with `EntryStatus = PendingPayment`
     and a real slot number, so the existing unique index still decides who gets the last seat
     (ADR #0004) and no second counter is needed. Free and existing entries are `Confirmed`.
   - Cons: Pending entries count as entries. They take seats until they confirm or expire, and
     anything that lists or seeds participants has to filter on `Confirmed`.
2. Create the participant only after the payment succeeds
   - Pros: The participant table only ever holds real entrants.
   - Cons: The seat isn't held during Checkout, so the tournament can fill while people are
     paying and every late payer needs a refund.

### For payments that arrive after the reservation is gone
1. Refund automatically (chosen)
   - Pros: The webhook finds no live reservation and refunds the charge in full. No manual
     work, and the payer is never charged for a seat they don't have.
   - Cons: The platform loses the Stripe processing fee on every refunded charge.
2. Confirm the entry if a seat is still free, refund otherwise
   - Pros: Fewer refunds.
   - Cons: Adds a race between reservation expiry and webhook arrival, and the outcome depends
     on timing.
3. Hold the money for manual review
   - Pros: Nothing is refunded by mistake.
   - Cons: Needs an admin workflow that doesn't exist.

### For changing entry fee and prizes
1. Lock the terms once money or entrants exist (chosen)
   - Pros: Entry fee and prizes can only change while the pool is `NotRequired` or `Unfunded`
     and the tournament has no entries, pending ones included. Funded escrow and paid entries
     always match the terms they were paid under.
   - Cons: To change terms after funding, an owner has to cancel and recreate the tournament.
2. Allow changes and reconcile the difference
   - Pros: More flexible for owners.
   - Cons: Needs top-ups and partial refunds against charges that are already made.

### For the cost of cancelling a paid tournament
1. The platform absorbs the Stripe fees, with no quota (chosen, for now)
   - Pros: Payers and the owner get full refunds, and there is nothing to track per charge.
   - Cons: Every cancellation costs the platform the processing fees, and nothing limits
     repeated cancellations.
2. The platform absorbs the fees, with a cancellation quota
   - Pros: Caps the fee loss and abuse.
   - Cons: Needs a counter, a window and a product decision about the limit.
3. Deduct the lost fees from the owner's refund
   - Pros: The platform loses nothing.
   - Cons: Needs the fee of each charge tracked.

## Decision
We keep `TournamentStatus` as it is and add `PrizePoolStatus` beside it. Tournaments with prizes
start `Unfunded`, tournaments without prizes are `NotRequired`, and a tournament can't be opened
until its pool is `NotRequired` or `Funded`. A tournament with an entry fee also needs its
owner's payout account `Enabled` before it can open.

Paying entrants reserve a seat as a `PendingPayment` participant, which reuses the slot-number
index instead of adding a new counter. A payment that arrives after its reservation is gone is
refunded in full automatically. Entry fee and prizes are locked as soon as the pool is past
`Unfunded` or any entry exists.

`JoinTournament` and `AddParticipant` return `Payment.PaidTournamentRequiresCheckout` on a
tournament with an entry fee, so free joins can't bypass payment.

For cancellations the platform absorbs the lost Stripe fees and there is no quota. This is a
temporary policy: it's the simplest one that makes refunds whole, and we'll revisit it, probably
with a quota, once real cancellation volume exists.

## Related ADRs
- ADR #0004 Max Participants Concurrency Control: pending entries reuse the unique slot-number
  index to arbitrate the last seat.
- ADR #0010 Payments Money Flow: this ADR is the paid entry lifecycle that ADR #0010 deferred, and
  it uses the `PrizePoolStatus` values defined there. The platform is merchant of record, which is
  why it carries the refund fees.
- ADR #0012 Stripe Webhook Ingestion Idempotency: webhooks can arrive late, duplicated and out of
  order, which is why late payments are refunded automatically.
