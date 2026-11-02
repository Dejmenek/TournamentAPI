# Glossary

Domain terms used across TournamentAPI. Use these words in code, tests, issues and ADRs.

## Paid tournaments

**Entry**
A user's place in a tournament, stored as a `TournamentParticipant` row. In a tournament with an
entry fee, the entry starts as `PendingPayment` and holds its slot until the payment succeeds or the
Checkout Session expires. Pending entries don't appear in participant lists, brackets or standings.

**Entry fee**
The amount a user pays to join a paid tournament. Entry fees go to the owner after settlement, minus
the platform fee.

**Prize pool**
The fixed EUR amounts the owner sets per placement. Its state is the tournament's `PrizePoolStatus`
(`NotRequired`, `Unfunded`, `Funded`, `PaidOut`, `Refunded`). A tournament with prizes can't open
until its prize pool is `Funded`.

**Escrow**
The owner's payment of `prize total + platform fee`, held in the platform's Stripe balance until it
is paid out to the winners or refunded. Paying the escrow is what moves the prize pool to `Funded`.

**Platform fee**
The platform's cut, a configurable percentage applied as `ceil(amount × pct)` to both the escrow and
the entry fees. It covers Stripe's processing fees.

**Settlement window**
The configurable wait between a tournament reaching `Completed` and its payouts running. Result
corrections are still allowed during the window. A correction that reopens matches cancels the
scheduled payouts, and they are scheduled again when the tournament completes again.

**Payout**
A Stripe transfer from the platform to a user's payout account: a prize to a winner, or entry fee
money to the owner. There is one payout per source charge.

**Ledger entry**
A `PaymentLedger` row for one outgoing transfer or refund. It is written with a unique business key
before the Stripe call, and its ID builds the Stripe idempotency key.

**Payout account**
The Stripe connected account a user needs to receive money, either as the owner of a tournament with
an entry fee or as a participant in a tournament with prizes. One per user. Users who only pay don't
need one. Stripe calls this a connected account; in this codebase it's a payout account.

**Payout account status**
The user's `PayoutAccountStatus`, visible only to that user. `NotStarted` means no payout account
exists yet. `Pending` means the account exists but onboarding isn't finished or Stripe is still
reviewing it. `Enabled` means the account's `transfers` capability is active and it can receive
payouts. `Restricted` means onboarding was submitted but Stripe has disabled transfers, usually
until more information is provided.
