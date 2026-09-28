---

description: "Task list for Round Robin Tournament Format"
---

# Tasks: Round Robin Tournament Format

**Input**: Design documents from `/specs/001-round-robin-bracket-format/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/graphql-schema-changes.graphql, quickstart.md

**Tests**: Included. Not explicitly requested in the spec, but this codebase has an established per-service/per-validation xUnit test convention (`BracketServiceTests.cs`, `MatchValidationsTests.cs`, etc.) and the Definition of Done requires both test suites to pass — new scheduling/ranking/validation logic gets the same coverage as existing logic of the same kind.

**Organization**: Tasks are grouped by user story (spec.md's US1/US2/US3) to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on an incomplete task)
- **[Story]**: Which user story this task belongs to (US1, US2, US3) — omitted for Setup/Foundational/Polish
- File paths are exact and relative to the repository root

---

## Phase 1: Setup

- [x] T001 [P] Author an ADR at `docs/adr/0007_round_robin_format_strategy.md` (use the `scaffold-adr` skill/existing ADR template in `docs/adr/`) documenting the `IBracketGenerationStrategy`/`IBracketCompletionStrategy` decision: the decision itself, the rationale, and the two rejected alternatives (inline `switch` on format; a generic plugin/rules engine) from research.md §1. Required by Constitution Principle VI before this feature is considered done.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story work can begin until this phase is complete — every story depends on `Tournament.Format` existing and the strategy dispatch mechanism being in place.

- [x] T002 [P] Create `TournamentAPI/Data/Models/TournamentFormat.cs`: `public enum TournamentFormat { SingleElimination, RoundRobin }` — `SingleElimination` is ordinal `0` (the default) and `RoundRobin` is ordinal `1`, per data-model.md.
- [x] T003 [P] Add a `Drawn` value to `TournamentAPI/Data/Models/MatchStatus.cs`, **appended after `NeedsReplay`** (`Scheduled = 0, Played = 1, NeedsReplay = 2, Drawn = 3`) — do NOT insert it between `Played` and `NeedsReplay`, since `MatchStatus` has no `HasConversion` in `ApplicationDbContext` and is stored as a plain `int`; inserting in the middle would shift `NeedsReplay`'s stored value (research.md §2).
- [x] T004 Add `public TournamentFormat Format { get; set; } = TournamentFormat.SingleElimination;` to `Tournament` in `TournamentAPI/Data/Models/Tournament.cs`. Depends on: T002.
- [x] T005 In `TournamentAPI/Tournaments/CreateTournamentInput.cs`, change the record to `public record CreateTournamentInput(string Name, DateTime StartDate, TournamentStatus Status, int MaxParticipants, TournamentFormat? Format = null);`. In `TournamentMutations.CreateTournament` (`TournamentAPI/Tournaments/TournamentMutations.cs`), set `Format = input.Format ?? TournamentFormat.SingleElimination` when constructing the new `Tournament` (FR-002). Do **not** add `Format` to `TournamentAPI/Tournaments/UpdateTournamentInput.cs` — format is immutable after creation (FR-012, research.md §3). Depends on: T004.
- [x] T006 [P] Create `TournamentAPI/Brackets/IBracketGenerationStrategy.cs`: `public interface IBracketGenerationStrategy { TournamentFormat Format { get; } Bracket CreateBracket(int tournamentId, IList<int> participantIds); }`.
- [x] T007 [P] Create `TournamentAPI/Brackets/IBracketCompletionStrategy.cs`: `public interface IBracketCompletionStrategy { TournamentFormat Format { get; } Task SyncCompletionAsync(ApplicationDbContext context, Tournament tournament, int bracketId, int frontierRound, CancellationToken token); }` (keeps the existing `frontierRound` parameter from `BracketCompletionService.SyncChampionAsync` so Single Elimination's call sites don't change shape; Round Robin's implementation ignores it).
- [x] T008 [P] Make `BracketService` (`TournamentAPI/Brackets/BracketService.cs`) implement `IBracketGenerationStrategy`, adding `public TournamentFormat Format => TournamentFormat.SingleElimination;`. No change to the existing `CreateBracket`/`CreateNextRoundMatches` logic. Depends on: T006.
- [x] T009 [P] Make `BracketCompletionService` (`TournamentAPI/Brackets/BracketCompletionService.cs`) implement `IBracketCompletionStrategy`: rename `SyncChampionAsync` to `SyncCompletionAsync` to match the interface signature, add `public TournamentFormat Format => TournamentFormat.SingleElimination;`. No change to the existing method body. Depends on: T007.
- [x] T010 [P] In `BracketMutations.GenerateBracket` (`TournamentAPI/Brackets/BracketMutations.cs`), replace the injected `BracketService bracketService` parameter with `IEnumerable<IBracketGenerationStrategy> generationStrategies`, and resolve `generationStrategies.Single(s => s.Format == tournament.Format)` before calling `.CreateBracket(tournamentId, participantIds)`. Depends on: T008.
- [x] T011 In `MatchMutations.Play` and `MatchMutations.CorrectMatchResult` (`TournamentAPI/Matches/MatchMutations.cs`), replace the injected `BracketCompletionService bracketCompletionService` parameter with `IEnumerable<IBracketCompletionStrategy> completionStrategies`, and resolve `completionStrategies.Single(s => s.Format == tournament.Format)` before calling `.SyncCompletionAsync(context, tournament, match.BracketId, frontierMatch.Round, token)` (`Play`) / `.SyncCompletionAsync(context, tournament, match.BracketId, frontierMatch.Round, token)` (`CorrectMatchResult`). Depends on: T009.
- [x] T012 Register both Single Elimination strategy implementations in `TournamentAPI/Program.cs`: `builder.Services.AddScoped<IBracketGenerationStrategy, BracketService>();` and `builder.Services.AddScoped<IBracketCompletionStrategy, BracketCompletionService>();` (alongside their existing concrete-type registrations, which stay as-is since `BracketMutations`/other call sites may still need the concrete types). Depends on: T008, T009.
- [x] T013 Gate the correction cascade to Single Elimination only (research.md §10 — this is a required correctness fix, not hardening: `MatchCascadePositionCalculator` is purely positional and will otherwise misfire on Round Robin's multi-round schedules). In `TournamentAPI/Matches/MatchCorrectionService.cs`, add a `bool allowCascade` parameter to `ApplyCorrectionAsync`; when `false`, write the audit row as today but return immediately instead of calling `PropagateAsync`. In `TournamentAPI/Matches/MatchMutations.cs`, pass `allowCascade: tournament.Format == TournamentFormat.SingleElimination` at both call sites (`Play`'s `isReplay` branch and `CorrectMatchResult`). Depends on: T004, T011.

**Checkpoint**: Foundation ready — `Tournament.Format` exists, both strategy interfaces exist with Single Elimination implementations wired through DI and the mutation call sites, and the correction-cascade bug is fixed. User story work can now begin.

---

## Phase 3: User Story 1 - Organizer runs a Round Robin tournament (Priority: P1) 🎯 MVP

**Goal**: An organizer can create a Round Robin tournament, generate a full schedule (every pair exactly once, one bye per participant when the count is odd), and record match results — including draws.

**Independent Test**: Create a tournament with `format: ROUND_ROBIN` and 5 participants, `generateBracket`, verify 10 matches covering every pair exactly once with one bye each; `play` one match decisively and one as a draw; confirm a Single Elimination match still rejects a null `winnerId`.

### Tests for User Story 1

> Write these first; they should fail to compile/pass until the corresponding implementation task lands.

- [x] T014 [P] [US1] Unit tests in `TournamentAPI.UnitTests/Services/RoundRobinBracketStrategyTests.cs`: even participant count (e.g. 6) → `N*(N-1)/2` matches, every pair appears exactly once, no byes; odd participant count (e.g. 5) → every pair appears exactly once AND every participant has exactly one bye across the schedule (FR-003/FR-004).
- [x] T015 [P] [US1] Unit tests in `TournamentAPI.UnitTests/Validations/MatchValidationsTests.cs`: `winnerId: null` with equal scores succeeds for a Round Robin match; `winnerId: null` with unequal scores fails; `winnerId: null` fails outright for a Single Elimination match regardless of scores (FR-005/FR-006).
- [x] T016 [P] [US1] Unit test in `TournamentAPI.UnitTests/Validations/BracketMutationValidationsTests.cs` confirming `ValidateEnoughParticipants` rejects a `generateBracket` attempt with fewer than 2 participants **regardless of tournament format** — this existing, format-agnostic validation already satisfies FR-011 for Round Robin by reuse, but nothing currently verifies that (identified during `/speckit-analyze`, finding E1).

### Implementation for User Story 1

- [x] T017 [US1] Create `TournamentAPI/Brackets/RoundRobinBracketStrategy.cs` implementing `IBracketGenerationStrategy` (`Format => TournamentFormat.RoundRobin`). Generate the schedule via the circle/polygon rotation method (research.md §4): fix one participant, rotate the rest across rounds; for an odd participant count, rotate a placeholder "bye" seat through the same rotation so every real participant sits out exactly one round. Each bye match sets `WinnerId` to the sole real participant and `Status = MatchStatus.Played`, matching `BracketService.CreateBracket`'s existing bye convention (FR-003/FR-004). Depends on: T006.
- [x] T018 [US1] Register the new strategy in `TournamentAPI/Program.cs`: `builder.Services.AddScoped<IBracketGenerationStrategy, RoundRobinBracketStrategy>();`. Depends on: T017, T012.
- [x] T019 [US1] In `TournamentAPI/Matches/MatchMutations.cs`, change the `winnerId` parameter of `Play` and `CorrectMatchResult` from `int` to `int?`. When `winnerId is null`, set `match.WinnerId = null` and `match.Status = MatchStatus.Drawn` (instead of `Played`); when `winnerId` is set, keep the existing `Played` behavior unchanged. Keep the existing `matchMetrics.MatchPlayed()` call firing in both cases (a draw is still "a match played", no new metric needed). Depends on: T003, T011, T013.
- [x] T020 [US1] Add `MatchErrorCodes.DrawNotAllowedForFormat` / `MatchErrors.DrawNotAllowedForFormat(int matchId)` and `MatchErrorCodes.DrawRequiresEqualScores` / `MatchErrors.DrawRequiresEqualScores(int matchId, int player1Score, int player2Score)` in `TournamentAPI/Matches/MatchErrorCodes.cs` and `TournamentAPI/Matches/MatchErrors.cs`, following the existing `ErrorBuilder.New().SetMessage(...).SetCode(...).SetExtension(...).Build()` pattern used by `MatchAlreadyPlayed`.
- [x] T021 [US1] In `TournamentAPI/Matches/MatchValidations.cs`, add `ValidateDrawAllowedForFormat(Tournament tournament, int? winnerId)` returning `MatchErrors.DrawNotAllowedForFormat(...)` when `winnerId is null && tournament.Format != TournamentFormat.RoundRobin` (FR-006), and `ValidateDrawHasEqualScores(int matchId, int? winnerId, int player1Score, int player2Score)` returning `MatchErrors.DrawRequiresEqualScores(...)` when `winnerId is null && player1Score != player2Score`. In `MatchMutations.Play`/`CorrectMatchResult`, call these two new validations, and only call the existing `ValidateWinnerIsParticipant`/`ValidateWinnerHasHigherScore` when `winnerId is not null`. Depends on: T019, T020.
- [x] T022 [US1] In `TournamentAPI/Matches/MatchValidations.cs`, update `ValidateMatchNotPlayed` and `ValidateMatchNotScheduled` to also treat `match.Status == MatchStatus.Drawn` the same as `MatchStatus.Played` (research.md §2: a drawn match is "already played" for both "don't replay it" and "it's eligible for correction" purposes). Depends on: T003.
- [x] T023 [US1] In `TournamentAPI/Brackets/BracketMutationValidations.cs`, add `ValidateFormatSupportsRoundAdvancement(Tournament tournament)` returning a new `BracketErrorCodes.RoundAdvancementNotSupportedForFormat` / `BracketErrors.RoundAdvancementNotSupportedForFormat(int tournamentId)` error when `tournament.Format != TournamentFormat.SingleElimination`. Wire it into `BracketMutations.UpdateRound` (`TournamentAPI/Brackets/BracketMutations.cs`) right after the existing bracket/owner checks — Round Robin's full schedule is generated upfront and has no next-round-from-winners concept (research.md §8). No change needed to `ValidateAllMatchesCompleted`/`ValidateNotFinalRound`, since Round Robin brackets never reach `UpdateRound`. Depends on: T010.
- [x] T024 [P] [US1] Create `TournamentAPI/Tournaments/WithdrawParticipantInput.cs`: `public record WithdrawParticipantInput(int TournamentId, int ParticipantId);`.
- [x] T025 [P] [US1] In `TournamentAPI/Tournaments/TournamentErrorCodes.cs` / `TournamentAPI/Tournaments/TournamentErrors.cs`, add error codes/builders for: caller is neither the tournament owner nor the participant being withdrawn (`NotAuthorizedForWithdrawal`), withdrawal attempted on a non-Round-Robin tournament, withdrawal attempted before a bracket exists, and the target participant not found — following the existing `ErrorBuilder` pattern in `TournamentErrors.cs`.
- [x] T026 [P] [US1] In `TournamentAPI/Tournaments/TournamentValidations.cs`, add `ValidateCanWithdrawParticipant(int ownerId, int callerId, int participantId, int tournamentId)` returning `TournamentErrors.NotAuthorizedForWithdrawal(...)` unless `callerId == ownerId || callerId == participantId` — FR-010 covers both a participant withdrawing themselves and the organizer removing them (research.md §6 Authorization; `/speckit-analyze` finding F1: an owner-only check would silently drop the "withdraws" half of FR-010). Also add `ValidateFormatSupportsWithdrawal(Tournament tournament)` (must be `TournamentFormat.RoundRobin`) and `ValidateBracketGeneratedForWithdrawal(bool bracketExists, int tournamentId)`. Uses the error builders from T025.
- [x] T027 [US1] Add a `WithdrawParticipant` mutation to `TournamentAPI/Tournaments/TournamentMutations.cs` (mirrors `JoinTournament`'s/`DeleteTournament`'s structure): load the tournament with its bracket, matches, and participants; validate it exists, `TournamentValidations.ValidateCanWithdrawParticipant` (owner OR the participant themselves — NOT `ValidateIsOwner`, which would wrongly exclude self-withdrawal), `ValidateFormatSupportsWithdrawal`, `ValidateBracketGeneratedForWithdrawal`, and that the target `TournamentParticipant` exists. For each of that participant's `Match` rows with `Status == MatchStatus.Scheduled`, set `Status = MatchStatus.Played` and `WinnerId` = whichever of `Player1Id`/`Player2Id` is **not** the withdrawing participant (FR-010 — a forfeit win for the opponent, standard win points per spec.md's Assumptions). Then call `context.TournamentParticipants.Remove(participant)` — `SoftDeleteInterceptor` converts this to `IsDeleted = true` automatically, exactly like `DeleteTournament`'s `context.Tournaments.Remove(tournament)` — and `SaveChangesAsync`. Depends on: T004, T024, T025, T026.
- [x] T028 [P] [US1] Unit tests in `TournamentAPI.UnitTests/Validations/TournamentValidationsTests.cs` for `ValidateCanWithdrawParticipant` (owner accepted, the withdrawing participant themselves accepted, an unrelated third user rejected), `ValidateFormatSupportsWithdrawal`, and `ValidateBracketGeneratedForWithdrawal` (all accept/reject cases). Depends on: T026.

### Integration tests for User Story 1

- [x] T029 [US1] Integration test in `TournamentAPI.IntegrationTests/GraphQL/Tests/Brackets/BracketMutationTests.cs`: `createTournament(format: ROUND_ROBIN)` → `joinTournament` × 5 → `generateBracket` → assert 10 matches, every pair exactly once, each participant appears in exactly one bye match (quickstart.md Scenario 1).
- [x] T030 [P] [US1] Integration test in `TournamentAPI.IntegrationTests/GraphQL/Tests/Matches/MatchMutationTests.cs`: on a Round Robin match, `play(winnerId: null, player1Score: 1, player2Score: 1)` succeeds with `status: DRAWN`; `play(winnerId: null, player1Score: 2, player2Score: 1)` is rejected (unequal scores) (quickstart.md Scenario 2, steps 2–3).
- [x] T031 [US1] Integration test in `TournamentAPI.IntegrationTests/GraphQL/Tests/Brackets/BracketMutationTests.cs`: `updateRound` against a Round Robin bracket is rejected with the new format-not-supported error (quickstart.md Scenario 5, step 3). Same file as T029 — sequential with it, not parallel.
- [x] T032 [P] [US1] Integration tests in `TournamentAPI.IntegrationTests/GraphQL/Tests/Tournaments/TournamentMutationTests.cs`: withdrawing a participant mid-Round-Robin-tournament (a) as the organizer and (b) as the participant themselves both turn their remaining `SCHEDULED` matches into forfeit wins for their opponents and remove them from `tournament.participants`; (c) calling it as an unrelated third user is rejected (quickstart.md Scenario 4).

**Checkpoint**: User Story 1 is independently functional — Round Robin tournaments can be created, scheduled, played (including draws and withdrawals by either the organizer or the participant), and Single Elimination's `Play`/`UpdateRound` behavior is unchanged for decisive results.

---

## Phase 4: User Story 2 - Organizer and participants track Round Robin standings (Priority: P2)

**Goal**: Standings (wins/draws/losses/points/rank) are computable at any point for a Round Robin tournament, and the tournament auto-completes with a champion once every match is decided.

**Independent Test**: Record a mix of results (including a draw and a forfeit) in a Round Robin tournament, query standings mid-tournament and confirm the ranking/points/tie-break are correct, then complete all matches and confirm the tournament closes out with the right champion (or no champion, if tied for first).

### Tests for User Story 2

- [x] T033 [P] [US2] Unit tests in `TournamentAPI.UnitTests/Services/StandingsServiceTests.cs`: `Points = Wins * 3 + Draws * 1` (FR-007); a two-way points tie is broken by head-to-head result between the tied participants (FR-008); a tie that head-to-head cannot resolve leaves both participants sharing the same `Rank` (FR-008); `MatchesPlayed` counts both `Played` and `Drawn` matches, `MatchesRemaining` counts `Scheduled` matches (data-model.md's `StandingEntry` table).

### Implementation for User Story 2

- [x] T034 [P] [US2] Create `TournamentAPI/Standings/StandingEntry.cs`: a plain class/output type with `ParticipantId`, `Wins`, `Draws`, `Losses`, `Points`, `Rank`, `MatchesPlayed`, `MatchesRemaining` (all `int`), per data-model.md's `StandingEntry` table.
- [x] T035 [US2] Create `TournamentAPI/Standings/StandingsService.cs` with `IReadOnlyList<StandingEntry> ComputeStandings(IReadOnlyList<Match> matches)`: derive `Wins`/`Draws`/`Losses`/`Points` per participant from `Match.WinnerId`/`Status` (a `Drawn` match adds one `Draws` to both sides; a `Played` match adds one `Wins` to the winner and one `Losses` to the other side, including forfeits), then rank per data-model.md's algorithm — group by `Points` descending, break ties within a group by summing points earned only from matches played among that group's members, and share `Rank` for any tie head-to-head cannot resolve. Depends on: T034.
- [x] T036 [P] [US2] Create `TournamentAPI/Standings/StandingsDataLoaders.cs` (`[DataLoaderGroup("StandingsBatchingContext")]`, mirroring `TournamentAPI/Brackets/BracketDataLoaders.cs`'s style): a `[DataLoader]` keyed by `BracketId` returning `Dictionary<int, IReadOnlyList<Match>>` with **all** matches for each bracket (not paginated — unlike `MatchDataLoaders.GetMatchesByBracketAsync`, which pages and is unsuitable for computing a full standings table).
- [x] T037 [US2] Create `TournamentAPI/Standings/StandingsResolvers.cs` (`[ObjectType<Bracket>]`, mirroring `TournamentAPI/Brackets/BracketResolvers.cs`'s `[Parent]` pattern): add a `standings` field that loads the bracket's matches via T036's loader and returns `standingsService.ComputeStandings(matches)` when the bracket's tournament format is `RoundRobin`, or an empty list for Single Elimination brackets. Depends on: T035, T036.
- [x] T038 [P] [US2] Create `TournamentAPI/Brackets/RoundRobinCompletionStrategy.cs` implementing `IBracketCompletionStrategy` (`Format => TournamentFormat.RoundRobin`): the bracket is complete when no `Match` in it has `Status == MatchStatus.Scheduled` (data-model.md's completion rule). Every time this strategy runs and that condition holds — including when it runs again because `CorrectMatchResult` changed an already-`Completed` tournament's data — **(re)compute** `tournament.ChampionId` from the *current* standings and set `tournament.Status = TournamentStatus.Completed`: `ChampionId` = the `ParticipantId` of the `StandingEntry` with `Rank == 1`, or `null` if more than one entry shares `Rank == 1`. Do **not** guard this with `if (tournament.Status != Completed)` — the champion must never go stale after a post-completion correction changes the standings (`/speckit-analyze` finding C1). Depends on: T035, T007.
- [x] T039 [US2] Register the new strategy in `TournamentAPI/Program.cs`: `builder.Services.AddScoped<IBracketCompletionStrategy, RoundRobinCompletionStrategy>();`. Depends on: T038, T012.

### Integration tests for User Story 2

- [x] T040 [P] [US2] Integration tests in `TournamentAPI.IntegrationTests/GraphQL/Tests/Standings/StandingsQueryTests.cs` (new folder/file): querying `bracket.standings` mid-tournament reflects only completed matches; after all matches are played, `tournament.status` is `COMPLETED` and `tournament.champion` is the `Rank == 1` participant (or unset on a tie) (quickstart.md Scenario 3).

**Checkpoint**: User Stories 1 and 2 are both independently functional — Round Robin tournaments can be played to completion with correct standings and champion determination.

---

## Phase 5: User Story 3 - Existing Single Elimination tournaments are unaffected (Priority: P3)

**Goal**: Confirm zero behavioral change for Single Elimination — this phase is regression verification, not new implementation (Foundational + US1 tasks already made the underlying code changes format-aware).

**Independent Test**: Run the existing `generateBracket` → `play` → `updateRound` flow end-to-end on a tournament created without specifying a format, and confirm every outcome matches pre-feature behavior.

### Tests for User Story 3

- [x] T041 [P] [US3] Integration test in `TournamentAPI.IntegrationTests/GraphQL/Tests/Tournaments/TournamentMutationTests.cs`: `createTournament` without a `format` argument results in `tournament.format == SINGLE_ELIMINATION`, and the full existing `generateBracket` → `play` → `updateRound` flow on it behaves identically to before this feature (FR-002/FR-013, quickstart.md Scenario 5, steps 1–2).
- [x] T042 [P] [US3] Integration test in `TournamentAPI.IntegrationTests/GraphQL/Tests/Matches/MatchMutationTests.cs`: `play(winnerId: null, ...)` against a Single Elimination match is rejected (regression boundary check distinct from US1's Round Robin draw tests) (FR-006/FR-013).

**Checkpoint**: All three user stories are independently functional. Existing Single Elimination test expectations are unmodified and green — any test that needed a behavior change to keep passing is a regression to fix, not a test to update (SC-003).

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T043 [P] Update the team's Postman collection (via the Postman MCP tools) to add: `format` on `createTournament`'s input, a nullable-`winnerId` example on `play` (including a draw), the new `withdrawParticipant` mutation (callable as either the organizer or the participant), and `standings` on `bracket` queries — required by the Definition of Done for any input/payload/query-shape change.
- [x] T044 Run `dotnet build`, `dotnet test TournamentAPI.UnitTests`, and `dotnet test TournamentAPI.IntegrationTests` from the repository root; fix any failures before considering the feature done (Definition of Done).
- [x] T045 [P] Regenerate `schema.graphql` (`dotnet run --project TournamentAPI`, or the project's existing schema-export step) and diff it against `specs/001-round-robin-bracket-format/contracts/graphql-schema-changes.graphql` to confirm the actual schema delta matches what was planned (including that `WithdrawParticipantPayload` has no `errors` field, per that contract's corrected shape).
- [x] T046 Walk through `specs/001-round-robin-bracket-format/quickstart.md`'s 5 scenarios end-to-end and check off its validation checklist.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately, and can run any time before Phase 6.
- **Foundational (Phase 2)**: No dependency on Setup. **Blocks all of Phase 3–5.**
- **User Stories (Phase 3–5)**: All depend on Foundational (Phase 2) completion.
  - US1 (P1) has no dependency on US2/US3.
  - US2 (P2) depends on US1's `RoundRobinBracketStrategy` (T017) existing so there are matches to compute standings from, but not on US1's draw/withdrawal tasks specifically.
  - US3 (P3) is pure regression verification of Foundational + US1's format-aware changes; it can run any time after US1's Phase 3 implementation tasks (T017–T028) land.
- **Polish (Phase 6)**: Depends on all desired user stories being complete.

### Within Each Phase

- Tests are listed before implementation and should be written first.
- Interfaces before implementations; implementations before DI registration; DI registration before the mutation that resolves the strategy at runtime.

### Parallel Opportunities

- Setup: T001 has no code dependency and can run any time.
- Foundational: T002+T003 in parallel; then T004 alone; then T006+T007 in parallel; then T008+T009 in parallel; then T010 (parallel with the T011→T013 chain, different files).
- US1: T014+T015+T016 in parallel; T024+T025+T026 in parallel (different files); T030+T032 in parallel with each other and with the T029→T031 chain (T029/T031 share a file).
- US2: T033+T034 in parallel; T036 in parallel with T035; T038 in parallel with T037 (both only depend on T035).
- US3: T041+T042 in parallel (different files).
- Polish: T043+T045 in parallel.

---

## Parallel Example: Foundational Phase

```bash
# Launch together:
Task: "Create TournamentAPI/Data/Models/TournamentFormat.cs"
Task: "Add Drawn value to TournamentAPI/Data/Models/MatchStatus.cs"

# Then, once T004 (Tournament.Format) is done:
Task: "Create TournamentAPI/Brackets/IBracketGenerationStrategy.cs"
Task: "Create TournamentAPI/Brackets/IBracketCompletionStrategy.cs"
```

## Parallel Example: User Story 1

```bash
# Tests, launched together:
Task: "Unit tests for round-robin schedule generation in TournamentAPI.UnitTests/Services/RoundRobinBracketStrategyTests.cs"
Task: "Unit tests for format-aware draw validation in TournamentAPI.UnitTests/Validations/MatchValidationsTests.cs"
Task: "Unit test for format-agnostic minimum-participant validation in TournamentAPI.UnitTests/Validations/BracketMutationValidationsTests.cs"

# Withdrawal groundwork, launched together:
Task: "Create TournamentAPI/Tournaments/WithdrawParticipantInput.cs"
Task: "Add withdrawal error codes/errors in TournamentAPI/Tournaments/TournamentErrorCodes.cs and TournamentErrors.cs"
Task: "Add withdrawal validations (including owner-or-self authorization) in TournamentAPI/Tournaments/TournamentValidations.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001 — can also be done later, it's documentation-only).
2. Complete Phase 2: Foundational (T002–T013) — **blocks everything else**.
3. Complete Phase 3: User Story 1 (T014–T032).
4. **STOP and VALIDATE**: run quickstart.md Scenarios 1, 2, and 4 manually or via the new integration tests.
5. This is a usable MVP: organizers (and participants themselves) can run a full Round Robin tournament (schedule, results, draws, withdrawal), even without a dedicated standings query (participants can still be told to tally `Match` results manually, or US2 follows immediately).

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. Add User Story 1 → test independently → Round Robin tournaments are playable (MVP).
3. Add User Story 2 → test independently → standings and auto-completion/champion determination work.
4. Add User Story 3 → regression-verify Single Elimination is untouched.
5. Polish (Postman, full test run, schema diff, quickstart walkthrough) → done.

## Notes

- [P] tasks touch different files and have no dependency on each other at the point they're ready to start.
- Every Foundational and US1/US2 implementation task cites the exact file(s) it touches and the research.md/data-model.md section backing its behavior — no task should require guessing a design decision that was already made in Phase 0/1.
- The correction-cascade fix (T013) is a **required correctness fix** uncovered during planning (research.md §10), not optional hardening — without it, correcting a Round Robin match's result can corrupt an unrelated match in the next round.
- The withdrawal mutation's owner-or-self authorization (T026/T027) and the always-recompute champion rule (T038) were both corrected during `/speckit-analyze` (findings F1 and C1) — the original drafts of these tasks were narrower than what FR-010/FR-009 actually require.
- Commit after each task or logical group; stop at either checkpoint to validate a story independently before moving on.

---

## Phase 7: Convergence

- [X] T047 [P] In the TournamentAPI Postman collection (via the Postman MCP tools), update the collection-level description's "Enums" and "Error codes" overview sections to list `MatchStatus.DRAWN`, the `TournamentFormat` enum, and the withdrawal error codes (`Tournament.NotAuthorizedForWithdrawal`, `Tournament.WithdrawalNotSupportedForFormat`, `Tournament.WithdrawalRequiresBracket`) and the draw error codes (`Match.DrawNotAllowedForFormat`, `Match.DrawRequiresEqualScores`) — the actual `format`/`withdrawParticipant`/draw/`standings` requests and their own descriptions are already correctly present (verified: "Withdraw Participant" request created 2026-09-27 with an accurate description, `createTournament`/`GetTournamentById` already include `format`/`standings`), only the collection's top-level overview blurb is stale (contradicts)
- [X] T048 In `TournamentAPI/Tournaments/TournamentMutations.cs`'s `WithdrawParticipant` mutation, after forfeiting the withdrawing participant's remaining `Scheduled` matches, resolve `IEnumerable<IBracketCompletionStrategy>` and call `.Single(s => s.Format == tournament.Format).SyncCompletionAsync(context, tournament, bracket.Id, frontierRound, token)` so that a withdrawal which clears the bracket's last `Scheduled` match correctly transitions `Tournament.Status` to `Completed` and (re)computes `ChampionId`, matching the always-recompute rule `RoundRobinCompletionStrategy` (T038) already applies from `Play`/`CorrectMatchResult` per FR-009 (missing)
- [X] T049 [P] Add a unit test in `TournamentAPI.UnitTests/Validations/BracketMutationValidationsTests.cs` for `ValidateFormatSupportsRoundAdvancement` (accepts Single Elimination, rejects Round Robin with `RoundAdvancementNotSupportedForFormat`), matching the per-validation unit test convention this feature's tests already apply to every other new validation (T016/T028/T033) per tasks.md's Tests section (partial)
- [X] T050 [P] Update `specs/001-round-robin-bracket-format/contracts/graphql-schema-changes.graphql`'s illustrated `MatchStatus` enum ordering from `SCHEDULED, PLAYED, DRAWN, NEEDS_REPLAY` to `SCHEDULED, PLAYED, NEEDS_REPLAY, DRAWN`, matching the actual (correct, append-only) implementation in `TournamentAPI/Data/Models/MatchStatus.cs` and `schema.graphql`, so the contract doc stays accurate (contradicts)
