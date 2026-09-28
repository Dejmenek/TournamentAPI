# Phase 1 Data Model: Round Robin Tournament Format

## New: `TournamentFormat` (enum)

`TournamentAPI/Data/Models/TournamentFormat.cs`

| Value | Meaning |
|---|---|
| `SingleElimination` (0, default) | Existing bracket-and-advance format. |
| `RoundRobin` (1) | Every participant plays every other participant exactly once. |

Stored as its underlying `int` on `Tournament`, matching how `TournamentStatus`/`MatchStatus` are already stored (no `HasConversion` in `ApplicationDbContext` today).

## Changed: `Tournament`

| Field | Change |
|---|---|
| `Format` | **New**, `TournamentFormat`, required, defaults to `SingleElimination`. Settable only at creation (`CreateTournamentInput.Format`, optional, defaults to `SingleElimination`); **not** present on `UpdateTournamentInput` — see research.md §3. |

No other fields change. `Bracket`/`ChampionId`/`Status` are reused unchanged for Round Robin (see below).

## Unchanged (reused as-is): `Bracket`

Still `{ Id, TournamentId, Matches }`. Acts as the generic "this tournament's set of matches" container regardless of format — no format-specific fields needed since format-specific behavior lives in the strategy services (research.md §1), not the entity.

## Changed: `MatchStatus` (enum)

`TournamentAPI/Data/Models/MatchStatus.cs`

| Value | Ordinal | Meaning |
|---|---|---|
| `Scheduled` (existing) | 0 | Not yet played. |
| `Played` (existing) | 1 | Decisive result — a win for one side, or a bye. `WinnerId` is always set. |
| `NeedsReplay` (existing) | 2 | Correction cascade flow, unchanged. |
| `Drawn` (**new**) | 3 | Round Robin only: match played with no winner. `WinnerId` is always null. |

`Drawn` is declared **after** `NeedsReplay` (appended, not inserted between `Played` and `NeedsReplay`) — `MatchStatus` has no `HasConversion` in `ApplicationDbContext`, so it's stored as a plain `int`, and inserting a value in the middle would shift `NeedsReplay`'s stored integer (research.md §2).

## Changed: `Match`

Still `{ Id, Round, BracketId, Player1Id, Player2Id?, WinnerId?, Player1Score, Player2Score, Status, RowVersion }` — no new columns, just the new `Status` value above. Valid states, format-aware:

| Status | WinnerId | Player2Id | Meaning |
|---|---|---|---|
| `Scheduled` | null | set | Not yet played (either format). |
| `Played` | set | null | Bye (either format). |
| `Played` | set | set | Decisive result (either format). |
| `Drawn` | **null** | set | **Draw — Round Robin only.** Single Elimination must never reach this state (FR-006). |
| `NeedsReplay` | (prior value) | set | Correction flow, unchanged. |

`Status == Drawn` is treated as "already played" everywhere `Status == Played` currently is for that purpose — see research.md §2's downstream impact list (`ValidateMatchNotPlayed`, `ValidateMatchNotScheduled`, Round Robin completion).

`Round` for Round Robin matches is the circle-method round number (research.md §4), not an elimination stage — same column, different meaning by format, no schema impact.

A **forfeit** (participant withdrawal, FR-010) is stored exactly like an ordinary decisive result: `Status = Played`, `WinnerId` = the remaining opponent, scores left at their default (`0`/`0`). Nothing distinguishes it from a normally-played match in storage (research.md §6).

## Changed: `TournamentParticipant`

No new fields. A withdrawal is an ordinary soft delete: `IsDeleted = true` (existing `ISoftDeletable` pattern, existing global query filter already excludes it from `tournament.Participants`).

## New (computed, not persisted): `StandingEntry`

A plain GraphQL output type, not an EF entity — projected on demand from a bracket's `Match` rows. Exposed via the new `Standings` feature folder.

| Field | Type | Notes |
|---|---|---|
| `ParticipantId` | `int` | |
| `Wins` | `int` | Includes forfeit wins. |
| `Draws` | `int` | |
| `Losses` | `int` | Includes forfeit losses. |
| `Points` | `int` | `Wins * 3 + Draws * 1` (FR-007). |
| `Rank` | `int` | 1-based; tied participants share a rank (FR-008) — e.g. two participants tied for 1st both get `Rank = 1`, next participant gets `Rank = 3`. |
| `MatchesPlayed` | `int` | Matches with `Status == Played` or `Status == Drawn` involving this participant. |
| `MatchesRemaining` | `int` | Matches with `Status == Scheduled` involving this participant. |

### Ranking algorithm (FR-007/FR-008)

1. Group by `Points` descending.
2. Within a points group of size > 1, order by head-to-head result: sum of points earned only from matches played among members of that same group.
3. If still tied after head-to-head, the tied participants share the same `Rank` (no further tie-break — matches the clarified answer).

### Completion (FR-009)

A Round Robin bracket is complete when every `Match` in it has `Status == Played` or `Status == Drawn` (i.e. no `Match` remains `Scheduled`). Whenever the completion strategy runs and this condition holds — including when it runs again because `CorrectMatchResult` changed an already-completed tournament's data — it (re)sets `Tournament.Status = Completed` and (re)computes `Tournament.ChampionId` from the *current* standings: the participant with `Rank == 1`, or `null` if more than one participant shares `Rank == 1` (mirrors Single Elimination's existing "no champion" fallback state rather than inventing a new one). The champion is never a one-time computation left stale after a later correction — it is recomputed from scratch every time completion is evaluated and the bracket is (still) complete.

## Relationships (unchanged)

`Tournament (1) —(0..1)→ Bracket (1) —(0..N)→ Match`, `Tournament (1) —(0..N)→ TournamentParticipant`. No new foreign keys.

## Migration

This project has no EF Core migrations at all — `Program.cs` and `TournamentAPI.IntegrationTests/BaseIntegrationTest.cs` both call `context.Database.EnsureCreatedAsync()`, which derives the schema directly from the current model on first creation. Adding `Tournament.Format` and `MatchStatus.Drawn` therefore requires no migration file — the next `EnsureCreatedAsync()` against a fresh database picks up both changes automatically. A developer with an existing local database from before this feature needs to drop it (or let their test/dev setup recreate it) to pick up the new `Format` column, since `EnsureCreatedAsync()` does not alter an already-created schema.
