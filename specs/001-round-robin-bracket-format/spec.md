# Feature Specification: Round Robin Tournament Format

**Feature Branch**: `001-round-robin-bracket-format`

**Created**: 2026-09-27

**Status**: Draft

**Input**: User description: "Currently our TournamentAPI supports only Single Elimination Bracket. We need to lay the groundwork to easily add new tournament types in the future and implement round-robin format"

## Clarifications

### Session 2026-09-27

- Q: Should each pair of participants in a Round Robin tournament play each other once, or twice (e.g. home-and-away)? → A: Single round — each pair meets exactly once.
- Q: Can an individual Round Robin match end in a draw, or must every match produce a winner? → A: Draws are allowed — a match can end with no winner.
- Q: When two or more participants finish a Round Robin tournament with the same number of points, how should the tie in final standings be broken? → A: Head-to-head result between the tied participants first; if still tied, they share the same rank.
- Q: If a participant withdraws or is removed from a Round Robin tournament after its schedule has already been generated, what should happen to their remaining scheduled matches? → A: Remaining matches are recorded as forfeits (losses) for the withdrawn participant.
- Q: Now that draws are allowed, how many points should a win, a draw, and a loss each be worth for standings ranking? → A: Win = 3 points, draw = 1 point, loss = 0 points.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organizer runs a Round Robin tournament (Priority: P1)

An organizer creating a new tournament wants to run it as a Round Robin instead of Single Elimination, so that every participant gets to play every other participant rather than being knocked out after one loss.

**Why this priority**: This is the core deliverable requested — without it, no new format exists and the feature has no user-facing value.

**Independent Test**: Can be fully tested by creating a tournament with Round Robin selected and a set of participants, then verifying a complete match schedule is generated where every participant faces every other participant exactly once.

**Acceptance Scenarios**:

1. **Given** an organizer is creating a tournament with 6 participants, **When** they select Round Robin as the format and start the tournament, **Then** the system generates a schedule containing every possible pairing of participants exactly once.
2. **Given** an organizer is creating a tournament with an odd number of participants, **When** the Round Robin schedule is generated, **Then** each participant receives exactly one bye and the rest of the schedule still pairs every participant with every other participant exactly once.
3. **Given** a Round Robin tournament has started, **When** a participant reports or an organizer records the result of one of their matches (a win for either side, or a draw), **Then** that result is saved and reflected in the tournament's ongoing standings.

---

### User Story 2 - Organizer and participants track Round Robin standings (Priority: P2)

An organizer or participant wants to see up-to-date standings (ranking, record, points) for a Round Robin tournament at any point, so they know who is leading and what is left to be decided.

**Why this priority**: Round Robin has no single elimination path to a winner; standings are how progress and the eventual result are communicated, so this is required to make the new format usable, but it builds on User Story 1 existing first.

**Independent Test**: Can be fully tested by recording a mix of match results in a Round Robin tournament and verifying the standings view reflects the correct ranking, records, and remaining matches at that point.

**Acceptance Scenarios**:

1. **Given** a Round Robin tournament where only some matches have been played, **When** the standings are viewed, **Then** they show each participant's current record based only on completed matches, ranked according to the tournament's ranking rules.
2. **Given** a Round Robin tournament where every scheduled match has been played, **When** the standings are viewed, **Then** the tournament is marked complete and a final ranking (including the overall winner) is shown.

---

### User Story 3 - Existing Single Elimination tournaments are unaffected (Priority: P3)

An organizer who creates a tournament without thinking about format, or who is already running a Single Elimination tournament, wants everything to keep working exactly as it does today.

**Why this priority**: Protects existing users and data; lower priority than delivering the new format itself, but a hard requirement before this feature can ship.

**Independent Test**: Can be fully tested by running the existing Single Elimination creation and match-progression flows end-to-end after this feature ships and confirming behavior, results, and champion determination are unchanged.

**Acceptance Scenarios**:

1. **Given** an organizer creates a tournament without explicitly choosing a format, **When** the tournament is created, **Then** it behaves exactly as a Single Elimination tournament does today.
2. **Given** an existing Single Elimination tournament created before this feature shipped, **When** its matches are played to completion, **Then** it is decided and completed the same way it was before this feature shipped.

---

### Edge Cases

- Starting a Round Robin tournament with fewer than 2 participants is rejected, using the same minimum-participant rule already enforced for every tournament today (see FR-011).
- A participant who withdraws (themselves) or is removed (by the organizer) from a Round Robin tournament after the schedule has already been generated has their remaining unplayed matches recorded as forfeits (losses), so standings stay complete and comparable for everyone else (see FR-010).
- When two or more participants are mathematically tied and some of their matches haven't been played yet, standings show them at the same rank based on results so far, using the same ranking and tie-break rules applied to a completed tournament (see FR-008, User Story 2 Acceptance Scenario 1).
- Changing a tournament's format after its schedule or bracket has already been generated is not possible — format is fixed at creation (see FR-012).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an organizer to choose a tournament format (Single Elimination or Round Robin) when creating a tournament.
- **FR-002**: System MUST default a tournament to Single Elimination when no format is explicitly chosen, so existing organizers and integrations see no change in behavior.
- **FR-003**: For a Round Robin tournament, system MUST generate a schedule in which every participant plays every other participant exactly once, in a single round (no rematches).
- **FR-004**: For a Round Robin tournament with an odd number of participants, system MUST give each participant exactly one bye across the schedule rather than leaving them unscheduled.
- **FR-005**: System MUST record the outcome of each Round Robin match individually as a win for one participant, a win for the other, or a draw.
- **FR-006**: System MUST allow a Round Robin match to end in a draw when neither participant wins. Single Elimination matches, in contrast, MUST always produce a winner, since advancing past a round depends on it.
- **FR-007**: System MUST calculate and expose standings for a Round Robin tournament at any point, ranking participants by total points earned: 3 points for a win, 1 point for a draw, 0 points for a loss (including a forfeit loss).
- **FR-008**: System MUST break ties in standings using head-to-head result between the tied participants first, and if the tie remains, by treating the participants as co-ranked (sharing the same position).
- **FR-009**: System MUST mark a Round Robin tournament as complete once every scheduled match has been played, and determine the final ranking and overall winner from the completed standings.
- **FR-010**: If a participant withdraws or is removed from a Round Robin tournament after its schedule has been generated, system MUST record each of that participant's remaining unplayed matches as a forfeit loss for them (and a corresponding win, worth the standard win points, for their scheduled opponent), so standings remain complete and comparable for everyone else.
- **FR-011**: System MUST prevent starting a Round Robin tournament with fewer than 2 participants, using the same minimum participant rule already enforced for tournaments today.
- **FR-012**: System MUST NOT allow a tournament's format to be changed once its schedule or bracket has been generated.
- **FR-013**: System MUST continue to support existing Single Elimination tournaments — creation, match progression, and champion determination — with no change in behavior after this feature ships.
- **FR-014**: System MUST be structured so that a new tournament format can be added in the future without requiring changes to the rules, data, or behavior of existing Single Elimination or Round Robin tournaments.

### Key Entities

- **Tournament**: Represents a competition run by an organizer; now additionally carries which format it uses (e.g., Single Elimination, Round Robin), decided at creation and fixed once the schedule/bracket is generated.
- **Format**: Represents the set of rules governing how a tournament's matches are generated, progressed, and resolved to a winner (e.g., Single Elimination's bracket-and-advance rules, Round Robin's play-everyone rules). Additional formats can be introduced over time.
- **Match**: A single contest between two participants (or a bye) within a tournament, with a round/grouping and a recorded outcome — a win for one side, or (for Round Robin only) a draw; used by both existing and new formats.
- **Standings**: A per-tournament, per-participant summary of results (wins, draws, losses, points, rank) used to communicate progress and determine the final outcome of formats — like Round Robin — that don't resolve via single-loss elimination.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An organizer can create a Round Robin tournament and see a complete, correct match schedule (every participant paired with every other participant exactly once) generated immediately upon starting the tournament.
- **SC-002**: At any point during a Round Robin tournament, standings shown to users are accurate as of the most recently recorded match result, with no stale or missing results.
- **SC-003**: 100% of existing Single Elimination tournament scenarios (creation, match progression, champion determination) continue to behave identically after this feature ships, with zero regressions.
- **SC-004**: A new tournament format can be added after this feature ships without modifying the rules, data, or behavior of previously created Single Elimination or Round Robin tournaments.
- **SC-005**: For a completed Round Robin tournament, the winner and full final ranking are unambiguous and derivable solely from recorded match results (including draws and forfeits), the published points system, and the tie-break rule.

## Assumptions

- The minimum participant count required to start a tournament (currently 2) applies equally to Round Robin tournaments; no additional Round Robin-specific minimum is introduced.
- A tournament's format is chosen once at creation time and cannot be changed after its schedule or bracket has been generated, consistent with how brackets already behave today.
- A forfeited match (from a participant withdrawal) counts as a standard win for the remaining opponent for standings purposes — the same points as a win achieved by playing the match.
