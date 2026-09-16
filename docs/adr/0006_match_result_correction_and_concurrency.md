# Title
Match Result Correction: Status Model, Cascade Propagation, and Concurrency Guards

# Date
15/09/2026

## Status
Accepted

## Context
`Play` sets a match's winner once: `WinnerId != null` is the only "already played" signal, and
`MatchValidations.ValidateMatchNotPlayed` locks the match permanently once it's set. There's no way to
correct a mistaken result. That's risky once `UpdateRound` has already built later rounds
from the wrong winner, since those later matches may themselves already be played on top of it, nothing
flags a match's result as untrustworthy, and nothing propagates a correction into rounds already built
from it.

Separately, `UpdateRound` reads a round's matches with a bare, unlocked `SELECT` and inserts the next
round from that snapshot with no check that the rows are still in the state they were read in. A
correction landing on one of those rows between the read and the insert's commit could silently bake a
stale winner into the next round, undetected.

## Considered Options

### For correcting a mistaken result
1. Allow re-calling `Play` on an already-played match
   - Pros: minimal change, no new mutation.
   - Cons: doesn't fix downstream matches already generated or played from the wrong winner; a bracket
     can end up with conflicting winners for the same slot across rounds, silently.
2. New `CorrectMatchResult` mutation, an explicit `Scheduled`/`Played`/`NeedsReplay` match status, and a
   positional cascade. (chosen)
   - Pros: makes "this result may now be wrong" an explicit, queryable state instead of silent
     corruption; reuses the pairing logic already in `BracketService.CreateNextRoundMatches`, so no new
     bracket-topology concept is introduced; never guesses a downstream outcome, it stops at the
     boundary of what's knowable and waits for a human to replay the invalidated match; gives a full
     audit trail for disputes.
   - Cons: more moving parts (new enum, mutation, cascade service, audit table); every existing
     match-construction path (`BracketService`, `DatabaseSeeder`, the benchmark seeder) needs to
     populate the new `Status` field.
3. Regenerate the entire bracket from round 1 on any correction
   - Pros: no positional cascade math needed.
   - Cons: destroys match history and scores for every round, including ones unrelated to the
     correction, the opposite of what issue #102 needs (a targeted correction with an audit trail, not
     a reset).

### For the `UpdateRound`-vs-correction race
1. Pessimistic locking (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`) on the round's matches while
   `UpdateRound` reads them
   - Pros: eliminates the race outright, holding the lock across the whole read-then-insert operation.
   - Cons: rejected previously in ADR #0004 for the same reasons: raw SQL Server lock hints aren't used
     anywhere else in this codebase and carry deadlock/contention risk under load, and the trade-offs
     haven't changed since that decision.
2. Re-affirm the round's matches as `Modified` and include them in `UpdateRound`'s single
   `SaveChangesAsync`, relying on the existing `[Timestamp]` concurrency token (chosen)
   - Pros: stays inside the optimistic-concurrency idiom this codebase already established for `Match`
     (reused again below for `CorrectMatchResult`); one atomic check, no new infrastructure, no lock
     hints, no raw SQL.
   - Cons: `UpdateRound` now bumps the `RowVersion` of every match it reads for that round, even when
     nothing else changed, which can produce a conflict for an unrelated concurrent reader or writer
     holding an older version token, accepted under the same "some wasted retries are fine" trade-off
     ADR #0002 already made for round advancement.

## Decision

### Status model
`Match` gets an explicit `Status` (`Scheduled`/`Played`/`NeedsReplay`), replacing `WinnerId != null` as
the "already played" signal. A new `CorrectMatchResult` mutation lets the tournament owner correct a
`Played` match's winner and scores. `Play` becomes tri-state: allowed on `Scheduled` or `NeedsReplay`,
blocked only on `Played`.

### Cascade propagation
Whenever a match's recorded winner changes, from either mutation, a single shared propagation routine
walks forward through the bracket using the same `(BracketId, Round)`-ordinal pairing `BracketService`
already uses to build rounds:

- It swaps the changed participant into the one downstream match at that position. A match has at most
  one downstream match, so this is always a single forward walk, never a tree.
- If that downstream match had already been decided (`Played` or `NeedsReplay`), it flips to
  `NeedsReplay` and the walk stops there. It never guesses who would have won a downstream match; a
  human has to replay the invalidated match, which re-triggers the same propagation for whatever comes
  after it.
- A downstream match that turns out to be a bye (single participant) is auto-advanced and the walk
  continues, since nothing was actually decided there, consistent with how byes are already
  auto-resolved today, at every round, by `BracketService`.
- Every match the walk touches gets a row in a new, internal-only `MatchCorrectionAudit` table, correlated by a
  per-invocation id and pointing at the match whose change caused it, for dispute investigation.
- The whole walk for one `CorrectMatchResult`/replay call is applied in one `SaveChangesAsync`, so no
  reader ever observes a half-applied cascade.

### Concurrency guard for corrections
`CorrectMatchResult` takes the match's `[Timestamp]` token, exposed to clients for the first time as a
base64-encoded `version` string alongside the underlying `RowVersion` column, and sets it as the EF
entity's original concurrency value before saving. That way a client acting on stale data gets a genuine
`DbUpdateConcurrencyException` even though the server itself re-read the row moments earlier. On that
conflict, we re-read the committed row: if it already matches exactly what was requested (same target
winner and scores), we treat it as a successful idempotent no-op, still logging a duplicate audit row,
rather than surfacing an error, since that shape means the client's own request already landed and it's
simply seeing its own prior success reflected back as a "conflict."

### Concurrency guard for `UpdateRound`
The round's matches it reads are marked `Modified` (a forced no-op reaffirmation) and included in the
same `SaveChangesAsync` that inserts the next round. If any of them changed underneath it, for example
via a concurrent correction, or even a concurrent legitimate `Play` call on another match in the same
round, the whole insert rolls back atomically and a new "round data changed since you read it, please
retry" error is reported. This is caught before, and is distinct from, the existing `DbUpdateException`
with unique-constraint guard that already protects against two concurrent `UpdateRound` calls generating
the same round twice. Both guards stay, since they protect against two different races.
