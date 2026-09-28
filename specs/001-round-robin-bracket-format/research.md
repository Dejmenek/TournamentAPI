# Phase 0 Research: Round Robin Tournament Format

## 1. Format extensibility mechanism

**Decision**: Add a `Format` field (`TournamentFormat` enum: `SingleElimination` = 0 default, `RoundRobin` = 1) to `Tournament`. Introduce a small strategy abstraction in the `Brackets` feature folder — `IBracketGenerationStrategy` (generates the initial set of matches for a tournament's participants) and `IBracketCompletionStrategy` (decides when a bracket is complete and who the champion is) — with one implementation per format, resolved at the call site via `IEnumerable<T>` + a `Format` property, filtered by `tournament.Format`. `BracketService`/`BracketCompletionService` become the `SingleElimination*` implementations (existing logic moved, not rewritten); `RoundRobinBracketStrategy`/`RoundRobinCompletionStrategy` are new.

**Rationale**: This is the minimum abstraction that satisfies FR-014 (new formats added later without touching existing ones) without introducing a generic plugin system nobody asked for. It's a real, non-trivial trade-off (a new interface + DI-based dispatch where a single concrete service existed before), so per Constitution Principle VI this decision **requires an ADR** — flagged as an early task in `tasks.md`, not written here since `/speckit-plan` doesn't author ADRs itself.

**Alternatives considered**:
- A `switch` on `tournament.Format` inline in `BracketMutations`/`MatchMutations` — rejected: it satisfies today's two formats but every future format re-touches the same mutation files, which is exactly what FR-014 rules out.
- A generic rules-engine/plugin-loader — rejected: massive over-engineering for two known formats; violates Constitution Principle III (minimal abstraction, no speculative infrastructure).

## 2. Representing a draw

**Decision**: Add a new `Drawn` value to the existing `MatchStatus` enum, declared **after** `NeedsReplay` (`Scheduled = 0, Played = 1, NeedsReplay = 2, Drawn = 3`) — appended at the end, not inserted between `Played` and `NeedsReplay`. A draw is `Status == Drawn`, `WinnerId == null`. `Played` continues to always mean a decisive result (`WinnerId` set — either a normal win or a bye).

**Rationale**: Keeps "was this match decided, and how" fully explicit in `Status` rather than inferred from a `WinnerId` null-check that would otherwise also have to be disambiguated from the `Scheduled` state. Every place in the codebase that currently branches on `MatchStatus` (completion checks, `ValidateMatchNotPlayed`, `ValidateMatchNotScheduled`, correction eligibility) gets one clear new case instead of a secondary null-check, which is easier to reason about and to test. Costs one enum value addition; no new column.

**Alternatives considered**:
- Nullable `WinnerId` + `Status == Played` meaning a draw (the original plan) — rejected per explicit direction: overloads one `Played` status to mean two different things (decisive result vs. draw), pushing the distinction into a second field-level check everywhere completion/validation logic reads `Status`.
- New `MatchOutcome` enum (`Win`, `Draw`) alongside `MatchStatus` — rejected: two enums tracking overlapping "is this match resolved" information is more state to keep consistent than one enum with an added value.
- New `bool IsDraw` column — rejected: redundant once `Status` already distinguishes it.
- Inserting `Drawn` between `Played` and `NeedsReplay` (matching its conceptual grouping) — rejected: `MatchStatus` has no `HasConversion` in `ApplicationDbContext`, so it's stored as a plain `int`; inserting a value in the middle silently shifts `NeedsReplay`'s underlying integer, which would corrupt any already-persisted `NeedsReplay` rows. Appending at the end costs nothing and avoids the risk entirely.

**Downstream impact of this decision**:
- `ValidateMatchNotPlayed` (blocks re-playing an already-decided match) must also reject `Status == Drawn`.
- `ValidateMatchNotScheduled` (used by `CorrectMatchResult` to require the match has already been played) must treat `Drawn` as "already played" alongside `Played`.
- Round Robin completion (`RoundRobinCompletionStrategy`, see data-model.md's completion rule) is "every match has `Status == Played` or `Status == Drawn`", not just `Played`.
- `Play` sets `Status = MatchStatus.Drawn` (not `Played`) when `winnerId == null`; `CorrectMatchResult` follows the same rule when correcting a result into or out of a draw.
- The GraphQL `MatchStatus` enum gains `DRAWN` (see `contracts/graphql-schema-changes.graphql`).

## 3. Format immutability

**Decision**: `Format` is settable only in `CreateTournamentInput` (defaults to `SingleElimination` when omitted) and is never exposed on `UpdateTournamentInput`. There is no code path that changes it after creation, not just after bracket generation.

**Rationale**: Trivially satisfies FR-012 ("MUST NOT allow a tournament's format to be changed once its schedule or bracket has been generated") with less validation code than allowing pre-generation edits. An organizer who picks the wrong format before generating a bracket can `deleteTournament` and recreate it — that mutation already exists.

**Alternatives considered**: Allowing `Format` on `UpdateTournamentInput` guarded by a "bracket not yet generated" check — rejected: extra validation surface for a case the spec doesn't call out as a required capability (Assumptions in spec.md already treat format choice as a one-time decision).

## 4. Round Robin schedule generation

**Decision**: Use the standard circle/polygon rotation method: fix one participant, rotate the rest around it across rounds. With an odd participant count, add a placeholder "bye" seat to the rotation so each real participant sits out exactly one round. This produces exactly `N-1` (even N) or `N` (odd N) rounds, with every pair meeting exactly once and, for odd N, every participant getting exactly one bye — matching FR-003/FR-004 directly.

**Rationale**: This is the standard, well-understood algorithm for round-robin scheduling; it satisfies the "exactly one bye each" requirement for free (a naive random-pairing-per-round approach doesn't guarantee even bye distribution). Round numbers are meaningful groupings (not elimination stages) and reuse the existing `Match.Round` column and the existing unique index `(BracketId, Round, Player1Id, Player2Id)` without changes.

**Alternatives considered**: Emitting all pairings under a single `Round = 1` — rejected: still technically unique per the existing index (pairs are unique by construction), but throws away useful grouping information the standings/UX story implies ("what's left to be decided" reads naturally per round).

## 5. Standings computation

**Decision**: Compute standings on demand from `Match` rows (wins/draws/losses/points per FR-007, head-to-head tie-break per FR-008) in a new `Standings` feature folder — not a persisted, denormalized table.

**Rationale**: Matches remain the single source of truth; nothing to keep in sync. A hand-written projection from `Match` to a `StandingEntry` output type is not a mapping library (Constitution Principle III is about banning generic mapping tools, not all projections) and follows the existing pattern of exposing a small, purpose-built type from the GraphQL layer.

**Alternatives considered**: A persisted `Standing`/`StandingsSnapshot` entity updated on every match write — rejected: adds a second source of truth and invalidation logic for a value that's cheap to compute per tournament (bounded by `MaxParticipants`).

## 6. Participant withdrawal / forfeit (FR-010)

**Decision**: No participant-removal mutation exists today for either format. Add one new mutation, scoped to Round Robin tournaments whose bracket has already been generated: it soft-deletes the `TournamentParticipant` row (reusing the existing `IsDeleted` soft-delete pattern) and marks each of that participant's remaining `Scheduled` matches as `Played` with `WinnerId` set to their scheduled opponent (a forfeit win, worth standard win points per the Assumptions in spec.md).

**Authorization**: FR-010 covers two distinct actors — a participant "withdraws" (self-service) "or is removed" (organizer-initiated). The mutation's caller MUST be either the tournament's owner or the participant being withdrawn themselves (`userId == tournament.OwnerId || userId == participantId`); anyone else is rejected. This was flagged during `/speckit-analyze` (finding F1) — an owner-only check would silently drop the "withdraws" half of FR-010's own wording.

**Rationale**: Matches FR-010 exactly, reuses the existing soft-delete convention (Constitution Principle IV) instead of inventing a new "withdrawn" flag, and requires no new Match fields (a forfeit is indistinguishable in storage from a normally decisive match — acceptable since the spec doesn't require surfacing "this was a forfeit" separately in standings).

**Alternatives considered**: Deleting the remaining scheduled matches outright — rejected explicitly by the clarification answer (forfeits, not removal). Generalizing this mutation to Single Elimination too — rejected: out of scope (spec's Edge Cases and FR-010 only cover Round Robin), and Single Elimination must show zero behavior change per FR-013. Owner-only authorization — rejected per the F1 analysis finding above, since it contradicts FR-010's own "withdraws or is removed" wording.

## 7. `Play` mutation changes for draws

**Decision**: Change `winnerId` from `Int!` to `Int` (nullable) on `PlayInput`/`Play`. Validation becomes format-aware: for Single Elimination, `winnerId == null` is rejected (existing behavior, unchanged); for Round Robin, `winnerId == null` is accepted only when `player1Score == player2Score`.

**Rationale**: Directly implements FR-005/FR-006. This is a breaking change to an existing required GraphQL argument, so per the Definition of Done the Postman collection must be updated in the same change, and the schema/input change belongs in `data-model.md` + `contracts/`.

**Alternatives considered**: A separate `PlayDraw` mutation — rejected: duplicates all the shared validation/concurrency/audit logic already in `Play` for no real benefit.

## 8. `UpdateRound` restriction

**Decision**: `UpdateRound` (advances a Single Elimination bracket to its next round from the current round's winners) gets one new validation: reject with a clear error if the bracket's tournament `Format != SingleElimination`. Round Robin brackets never call it — their full schedule is generated upfront.

**Rationale**: Round Robin has no "advance to next round from winners" concept; a full schedule already exists after `GenerateBracket`. Rejecting explicitly (rather than leaving it to accidentally misbehave) keeps the format boundary explicit and testable, satisfying FR-013's "no change in behavior" guarantee for Single Elimination while making the Round Robin restriction discoverable.

## 9. Performance / scale

**Decision**: No new participant cap for Round Robin. The existing `Tournament.MaxParticipants` bound (already enforced for every tournament, min 2, no stated max) is reused as-is; Round Robin's `O(n²)` match count is accepted at whatever bound organizers already choose today.

**Rationale**: The spec sets no explicit new performance/scale target (confirmed during `/speckit-clarify` — flagged Outstanding/low-impact), and introducing a Round-Robin-specific cap would be a new, unrequested configuration surface. Existing schedule generation is a single in-memory loop over participants (see `BracketService.CreateBracket`), so `O(n²)` match rows for realistic tournament sizes (tens of participants) is not a practical concern.

**Alternatives considered**: Adding a lower `MaxParticipants` ceiling specifically for Round Robin — rejected: no requirement or evidence motivates a different limit than what organizers already work within today.

## 10. Correction cascade must not run for Round Robin

**Discovery**: `MatchCorrectionService.ApplyCorrectionAsync` unconditionally calls `PropagateAsync`, which uses `MatchCascadePositionCalculator.GetDownstreamMatchId` to find a "downstream match" in `Round + 1` purely by list position (`position / 2`) — it has no awareness of tournament format. A Round Robin bracket has multiple rounds too (research.md §4), so correcting a Round Robin match's result would spuriously compute a positional "downstream match" in the next round and overwrite its `Player1Id`/`Player2Id`/`Status` (potentially flipping it to `NeedsReplay`) even though Round Robin matches have no such advancement relationship. This is only latent today because `winnerId` was never nullable and Round Robin didn't exist; it becomes a real correctness bug the moment `CorrectMatchResult` is used on a Round Robin match.

**Decision**: `MatchMutations.CorrectMatchResult` (and the `isReplay` correction path inside `Play`) must only invoke `MatchCorrectionService`'s cascade propagation when `tournament.Format == SingleElimination`. For Round Robin, applying a correction updates only the corrected match itself (audit row still written) — standings are computed on demand from `Match` rows, so no downstream match update is ever needed.

**Rationale**: Prevents an existing single-elimination-only mechanism from corrupting unrelated Round Robin matches based on positional coincidence. This is a required correctness fix enabling Round Robin corrections at all, not an optional hardening step.

**Alternatives considered**: Making `MatchCascadePositionCalculator` format-aware itself — rejected: the calculator has no access to `Tournament` and shouldn't need it; the cheaper, more legible fix is to not call it at all for Round Robin at the mutation call site.
