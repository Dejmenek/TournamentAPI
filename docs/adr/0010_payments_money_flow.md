# Title
Payments Money Flow for Paid Tournaments

# Date
02/11/2026

## Status
Accepted

## Context
Owners want to run paid tournaments (#172): an entry fee, cash prizes, both, or neither. Money comes
from several payers (the owner and every paying entrant) and goes to several payees (the owner and
the winners), and nobody knows who the winners are until the tournament ends. Result corrections
are still allowed after `Completed` (ADR #0006), so the final placements aren't certain until some
time after that.

Stripe Connect has three charge types, and they decide who holds the money while the tournament
runs, who is liable for refunds and disputes, and how many accounts one payment can reach. We also
have to decide who funds the prizes.

The integration only runs in Stripe test mode and sandboxes. Going live needs work that is out of
scope here: Stripe treats skill contests with monetary prizes as a restricted business that needs
prior approval, and some jurisdictions regulate entry-fee prize pools like gambling.

## Considered Options

### For the charge type
1. Separate charges and transfers (chosen)
   - Pros: The platform charges every payer and holds the funds in its own balance until the
     results are final, then sends any number of transfers to any connected account, grouped with
     `transfer_group = tournament-{id}`. This is the only charge type that lets one escrow charge
     pay several winners.
   - Cons: The platform is the merchant of record, so it carries refunds, disputes and Stripe fees.
     Transfers only work within one region, so connected accounts are limited to the EU/EEA. Every
     outgoing transfer needs its own bookkeeping (a ledger) because Stripe idempotency keys only
     last about 24 hours.
2. Destination charges
   - Pros: Stripe moves the funds to the connected account as part of the charge, so there are no
     separate transfers to track.
   - Cons: One charge has exactly one destination, chosen when the payer pays. The winners aren't
     known at that point, and a prize pool has to be split between several of them.
3. Direct charges
   - Pros: The connected account is the merchant of record and carries disputes and refunds.
   - Cons: The money lands in the owner's account, and the platform can't move it to the winners.
     Every owner's account would have to accept card payments, not only receive transfers.

### For prize funding
1. Owner-funded prizes held in escrow (chosen)
   - Pros: The owner sets a fixed EUR amount per placement and pays `prize total + platform fee`
     before the tournament can open, so the prizes are guaranteed and don't depend on how many
     people join. Entry fees stay separate and go to the owner after settlement.
   - Cons: The owner pays up front. The tournament gets a new `PrizePoolStatus` lifecycle
     (`NotRequired`, `Unfunded`, `Funded`, `PaidOut`, `Refunded`) next to `TournamentStatus`.
2. Prizes paid out of the pooled entry fees
   - Pros: The owner pays nothing up front.
   - Cons: The prize depends on turnout, so it isn't known when people join. Entry-fee prize pools
     are the arrangement some jurisdictions regulate like gambling.

## Decision
We use separate charges and transfers with owner-funded prizes. The platform charges every payer
through Stripe Checkout and holds the money until the settlement window closes. It then sends one
transfer per source charge (`source_transaction`): prize transfers are funded from the escrow
charge, and the owner gets one transfer per entry-fee charge.

- One currency (EUR). Connected accounts are limited to the EU/EEA.
- All amounts are `long` minor units (cents).
- The platform fee is a configurable percentage, `ceil(amount × pct)`, applied to both the escrow
  and the entry fees. It covers Stripe's processing fees. Winners receive the face value of their
  prize.
- Single elimination pays either 1st and 2nd, or 1st only. Round robin pays ranks 1..N from the standings
  (ADR #0007). Tied ranks split the combined prize money for the places they cover equally, and
  leftover cents go to the tied users in ascending user ID order.
- All four combinations of entry fee and prizes are allowed. Free tournaments without prizes work
  exactly as they do today.

Being merchant of record is the trade-off we accept to pay several winners from one pool. Refunds,
disputes and the ledger are covered by later ADRs (#0011 paid entry lifecycle, #0013 settlement and
payouts).

## Related ADRs
- ADR #0006 Match Result Correction and Concurrency: corrections after `Completed` are why the
  platform has to hold the money until a settlement window passes instead of paying out at once.
- ADR #0007 Round Robin Format Strategy: round robin prize placements are read from its standings.
