# Quickstart: Validating Round Robin Tournament Format

## Prerequisites

- `dotnet build` succeeds.
- API running locally: `dotnet run --project TournamentAPI` (GraphQL endpoint at `/graphql`).
- An authenticated user (register/login via the existing `registerUser`/`loginUser` mutations) to act as the organizer, and 4+ additional users to act as participants — same setup as any existing Bracket/Match integration test.

## Scenario 1 — Round Robin schedule generation (User Story 1, FR-003/FR-004)

1. `createTournament` with `format: ROUND_ROBIN`, `maxParticipants: 5`.
2. `joinTournament` for 5 participants.
3. `generateBracket(input: { tournamentId })`.
4. **Expect**: the returned `Bracket.matches` contains exactly `5 * 4 / 2 = 10` matches, every pair of participants appears exactly once, and each participant appears in exactly 4 matches across the schedule with one round where they have no opponent (bye) — since 5 is odd.

## Scenario 2 — Recording results, including a draw (User Story 1, FR-005/FR-006)

1. Using the bracket from Scenario 1, call `play(input: { matchId, winnerId: <participantId>, player1Score: 2, player2Score: 1 })` for one match.
   - **Expect**: succeeds, match `status` becomes `PLAYED`.
2. Call `play` on a different match with `winnerId: null, player1Score: 1, player2Score: 1`.
   - **Expect**: succeeds, match `status` becomes `DRAWN` (not `PLAYED`) and `winnerId` stays null.
3. Call `play` with `winnerId: null` but unequal scores.
   - **Expect**: rejected with a validation error (draw requires equal scores).
4. Repeat step 2's null-`winnerId` call against a **Single Elimination** tournament's match.
   - **Expect**: rejected — Single Elimination matches always require a winner (regression check for FR-006/FR-013).

## Scenario 3 — Standings (User Story 2, FR-007/FR-008)

1. Query the bracket's `standings` after only some matches are played.
   - **Expect**: entries only reflect completed matches; `points = wins*3 + draws*1`; participants with no completed matches show `points: 0`.
2. Play all remaining matches to completion.
   - **Expect**: `Tournament.status` becomes `COMPLETED`, `Tournament.champion` is set to the participant with `rank: 1` (or left unset if multiple participants share `rank: 1`), and `standings` reflects the final table with head-to-head tie-breaks applied per FR-008.

## Scenario 4 — Withdrawal / forfeit (FR-010)

1. With the Scenario 1 bracket in progress (some matches still `SCHEDULED`), call the new withdrawal mutation as the **organizer**, targeting one participant.
2. **Expect**: that participant's remaining `SCHEDULED` matches become `PLAYED` with their scheduled opponent as `winnerId`; `standings` shows the opponents' forfeit wins; the withdrawn participant no longer appears in `tournament.participants`.
3. Repeat against a different participant, this time calling the mutation **as that participant themselves** (not the organizer).
   - **Expect**: succeeds identically to step 1 — FR-010 covers both a participant withdrawing themselves and the organizer removing them (research.md §6 Authorization).
4. Call the mutation as a third, unrelated user (neither the organizer nor the target participant).
   - **Expect**: rejected — only the organizer or the participant being withdrawn may call it.

## Scenario 5 — Single Elimination regression (User Story 3, FR-013, SC-003)

1. Run the existing `generateBracket` → `play` → `updateRound` flow end-to-end on a tournament created **without** specifying `format` (or explicitly `SINGLE_ELIMINATION`).
2. **Expect**: byte-for-byte the same behavior as before this feature — champion determination via `BracketCompletionService`'s existing single-final-match logic, `updateRound` still advances winners.
3. Call `updateRound` against a **Round Robin** bracket instead.
   - **Expect**: rejected with the new "format not supported" error (FR-013 boundary check).

## Validation checklist

- [x] All 5 scenarios pass manually via GraphQL (or as integration tests under `TournamentAPI.IntegrationTests/GraphQL/Tests/`).
- [x] `dotnet test TournamentAPI.UnitTests` passes (new: Round Robin schedule generator, standings ranking algorithm, format-aware `Play` validations, minimum-participant validation applies regardless of format).
- [x] `dotnet test TournamentAPI.IntegrationTests` passes (existing Single Elimination suite unmodified and green; new Round Robin suite added).
- [x] Postman collection updated for: `CreateTournamentInput.format`, `PlayInput.winnerId` nullability, new withdrawal mutation, new `Bracket.standings` query.
- [x] ADR added under `docs/adr/` for the format-strategy abstraction (research.md §1).
