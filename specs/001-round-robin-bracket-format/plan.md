# Implementation Plan: Round Robin Tournament Format

**Branch**: `001-round-robin-bracket-format` | **Date**: 2026-09-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-round-robin-bracket-format/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

TournamentAPI supports only Single Elimination today. This feature adds a `Format` choice on `Tournament` (`SingleElimination` default, `RoundRobin`), lays a small strategy abstraction under the existing `Brackets` feature so a future format doesn't require touching Single Elimination or Round Robin code, and implements Round Robin: full round-robin scheduling (circle method, one bye per odd-count schedule), draws (a new `Drawn` `MatchStatus` value), a computed standings/points system (3/1/0, head-to-head tie-break, shared rank), and forfeit handling for mid-tournament withdrawal. Single Elimination's existing behavior is unchanged (verified by regression, not rewrite).

## Technical Context

**Language/Version**: .NET 9 / C# 13 (existing stack, no change)

**Primary Dependencies**: HotChocolate v16 (GraphQL, implementation-first, type-extension), EF Core 9 + SQL Server, ASP.NET Core Identity + JWT bearer auth (existing stack — no new dependency needed for this feature)

**Storage**: SQL Server via EF Core 9. No migrations exist in this repo — schema comes from `Database.EnsureCreatedAsync()` (`Program.cs`, `BaseIntegrationTest.cs`), so both model changes (`Tournament.Format`, `MatchStatus.Drawn`) take effect on the next fresh database creation with no migration file needed (see data-model.md)

**Testing**: xUnit for `TournamentAPI.UnitTests` (schedule generator, standings ranking, format-aware validations), xUnit + TestContainers for `TournamentAPI.IntegrationTests` (GraphQL end-to-end, mirroring the existing `Brackets`/`Matches` test folders)

**Target Platform**: ASP.NET Core web API (existing modular monolith deployable)

**Project Type**: web-service (single backend, no frontend/mobile project in this repo)

**Performance Goals**: None beyond what's already enforced. Round Robin's match count is `O(n²)` in participant count; reuses the existing `Tournament.MaxParticipants` bound rather than introducing a new one (research.md §9)

**Constraints**: Format is immutable after tournament creation (research.md §3); Single Elimination must show zero behavioral change (FR-013/SC-003)

**Scale/Scope**: Same organizer/participant scale as today's Single Elimination tournaments — no new scale target introduced by this feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|---|---|---|
| I. GraphQL-First, Type-Extension | New `Standings` capability is its own `[ExtendObjectType]` feature folder; `Tournament`/`Bracket`/`Match` remain entities-as-types with `[GraphQLIgnore]` for non-schema fields. No new DTO/mapping layer. | PASS |
| II. Validation Without Exceptions | All new rules (format-aware draw validation, withdrawal eligibility, `updateRound` format rejection) are static functions returning `IError?`, composed via `TryReportError`, matching `TournamentValidations`/`MatchValidations`/`BracketMutationValidations` today. | PASS |
| III. Minimal Abstraction | The one new abstraction (`IBracketGenerationStrategy` / `IBracketCompletionStrategy`) is justified by an explicit requirement (FR-014) and two concrete implementations, not speculative. No repository layer, no AutoMapper — standings are a hand-written projection. | PASS — see Complexity Tracking (ADR required, not a violation) |
| IV. Optimistic Concurrency & Soft Delete | Withdrawal reuses `TournamentParticipant.IsDeleted` (no new delete path); `Match.RowVersion` concurrency handling in `Play`/`CorrectMatchResult` is unchanged. | PASS |
| V. Explicit Types & Dependencies | `winnerId` becomes nullable `int?` end-to-end (real nullability signal, not suppressed); a draw is a distinct `MatchStatus.Drawn` value rather than an overloaded null check; new files write explicit `using` directives per project convention. | PASS |
| VI. Documented Trade-offs | Format-strategy abstraction is a genuine trade-off → ADR required (tracked in Complexity Tracking below and `quickstart.md`'s checklist). | ACTION REQUIRED (ADR, not a gate failure) |

No gate failures. One documented follow-up obligation (ADR) tracked below.

**Post-Phase-1 re-check**: table above already reflects the full Phase 1 design (`Standings` feature folder, strategy interfaces, withdrawal mutation) produced in data-model.md/contracts/quickstart.md — no new principle concerns surfaced during design; re-check holds.

## Project Structure

### Documentation (this feature)

```text
specs/001-round-robin-bracket-format/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
TournamentAPI/
├── Data/Models/
│   ├── Tournament.cs                    # + Format field
│   ├── TournamentFormat.cs              # NEW enum
│   └── MatchStatus.cs                   # + Drawn value
├── Brackets/
│   ├── BracketService.cs                # → becomes SingleElimination strategy impl
│   ├── BracketCompletionService.cs      # → becomes SingleElimination completion impl
│   ├── IBracketGenerationStrategy.cs    # NEW
│   ├── IBracketCompletionStrategy.cs    # NEW
│   ├── RoundRobinBracketStrategy.cs     # NEW — circle-method schedule generation
│   ├── RoundRobinCompletionStrategy.cs  # NEW — all-matches-played + standings-based champion
│   ├── BracketMutations.cs              # generateBracket dispatches by Format; updateRound rejects RoundRobin
│   └── BracketMutationValidations.cs    # + format-aware validation
├── Matches/
│   ├── MatchMutations.cs                # Play/CorrectMatchResult: nullable winnerId; skip correction cascade for Round Robin (research.md §10)
│   ├── MatchValidations.cs              # + draw validation (format-aware)
│   └── MatchCorrectionService.cs        # cascade propagation gated to Single Elimination only
├── Standings/                           # NEW feature folder
│   ├── StandingEntry.cs                 # computed output type
│   ├── StandingsService.cs              # ranking algorithm (points, head-to-head, shared rank)
│   ├── StandingsDataLoaders.cs          # all-matches-by-bracket loader (non-paginated)
│   └── StandingsResolvers.cs            # Bracket.standings field
├── Tournaments/
│   ├── CreateTournamentInput.cs         # + optional Format
│   ├── TournamentMutations.cs           # NEW WithdrawParticipant mutation
│   └── TournamentValidations.cs         # + withdrawal eligibility checks

TournamentAPI.UnitTests/
├── Services/
│   ├── RoundRobinBracketStrategyTests.cs   # NEW
│   └── StandingsServiceTests.cs            # NEW
└── Validations/
    ├── MatchValidationsTests.cs            # + draw cases
    └── TournamentValidationsTests.cs       # + withdrawal cases

TournamentAPI.IntegrationTests/GraphQL/Tests/
├── Brackets/BracketMutationTests.cs        # + Round Robin generateBracket, updateRound-rejects cases
├── Matches/MatchMutationTests.cs           # + draw cases (both formats)
├── Standings/StandingsQueryTests.cs        # NEW
└── Tournaments/TournamentMutationTests.cs  # + withdrawal cases
```

**Structure Decision**: Single web-service project (existing modular monolith). No new project is added to the solution — this feature extends the existing `Brackets`, `Matches`, and `Tournaments` feature folders and adds one new feature folder (`Standings`), consistent with the "one feature per folder" convention.

## Complexity Tracking

No Constitution gate failures. One documented obligation, not a violation:

| Item | Why Needed | Simpler Alternative Rejected Because |
|------|------------|---------------------------------------|
| `IBracketGenerationStrategy` / `IBracketCompletionStrategy` (new abstraction, Principle VI requires an ADR) | FR-014 requires that a future third format not require changing Single Elimination or Round Robin code; a two-implementation strategy interface is the minimum indirection that achieves that. | An inline `switch (tournament.Format)` in `BracketMutations`/`MatchMutations` was considered simpler today, but every future format would then re-edit those same mutation files — directly violating FR-014. |
