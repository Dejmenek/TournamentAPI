# Title
Concurrency Control for Tournament MaxParticipants

# Date
04/09/2026

## Status
Accepted

## Context
`Tournament` now has a `MaxParticipants` cap (see issue #95). `JoinTournament` and `AddParticipant`
both read the current participant count in memory and then insert a new `TournamentParticipant`
row in a separate step. Two concurrent requests can both read the same "count < MaxParticipants"
state before either commits, allowing the tournament to be over-filled — a classic check-then-act
race condition.

## Considered Options
1. Pessimistic Locking (e.g. `SELECT ... WITH (UPDLOCK, HOLDLOCK)` / explicit serializable transaction)
   - Pros: Guarantees correctness by serializing access to the tournament's participant count.
   - Cons: Introduces contention and potential deadlocks under load, since every join to the same
     tournament is serialized; requires manual transaction/lock-hint management not used elsewhere
     in this codebase.
2. Denormalized Counter + Atomic Update (e.g. `UPDATE Tournaments SET ParticipantCount = ParticipantCount + 1 WHERE ParticipantCount < MaxParticipants`)
   - Pros: Single atomic statement, no explicit locking needed, cheap to check capacity.
   - Cons: Requires keeping a redundant counter in sync with the `TournamentParticipant` table on
     every insert and soft-delete path — a new failure mode if the two ever drift; needs an
     explicit transaction wrapping the update and the insert, a shape not used elsewhere in this
     codebase's mutations.
3. Slot Numbers + Unique Database Index (chosen)
   - Pros: Reuses the pattern already established for `Match` (`BracketId, Round, Player1Id,
     Player2Id` unique index) and for the existing composite-PK `UserAlreadyParticipant` race
     handling; no extra locking or redundant state — the database itself is the single source of
     truth for whether a slot is free.
   - Cons: Requires distinguishing which of two unique constraints was violated when a
     `DbUpdateException` occurs, solved by inspecting the SQL exception message for the
     constraint/index name.

## Decision
We added a `SlotNumber` column to `TournamentParticipant` and a unique index on
`(TournamentId, SlotNumber)`. `JoinTournament`/`AddParticipant` still perform the existing
in-memory capacity check first (fast path, produces a clear `Tournament.Full` error for the common
case), then compute `SlotNumber = Participants.Count + 1` and insert. Under a genuine race, the
unique index arbitrates: exactly one insert succeeds, and the other throws a `DbUpdateException`
that we catch and translate into the same `Tournament.Full` error, distinguished from the existing
`UserAlreadyParticipant` race (which uses the `TournamentId, ParticipantId` primary key) by
inspecting which constraint/index name appears in the underlying SQL exception message.
