# Title
Bracket Format Extensibility: Generation/Completion Strategy Interfaces

# Date
27/09/2026

## Status
Accepted

## Context
`Tournament` supported exactly one bracket format, Single Elimination, and the code reflected that:
`BracketMutations.GenerateBracket` called `BracketService.CreateBracket` directly, and `MatchMutations`
called `BracketCompletionService.SyncChampionAsync` directly, with no notion of "format" anywhere in the
model. Adding Round Robin as a second format means both call sites now need to pick behavior based on
the tournament's format, and the feature's own requirement (FR-014) says a third format, whenever it
shows up, must not require re-editing the code that already works for Single Elimination or Round Robin.
That rules out solving this one branch at a time.

## Considered Options
1. Inline `switch (tournament.Format)` in `BracketMutations.GenerateBracket` and `MatchMutations.Play`/`CorrectMatchResult`
   - Pros: no new types; smallest possible diff for exactly two formats.
   - Cons: every future format adds another `case` to these same mutation files, which is precisely
     what FR-014 rules out; the switch would need to be duplicated (or centrally shared and threaded
     through) anywhere else format-specific bracket behavior is needed, growing the same coupling over
     time.
2. A generic rules-engine/plugin-loader for tournament formats
   - Pros: maximally extensible; formats could in principle be registered without a recompile.
   - Cons: no third format is planned or requested; this is speculative infrastructure for a
     requirement (FR-014) that only asks for isolation between formats, not runtime pluggability,
     violating Constitution Principle III (minimal abstraction).
3. `IBracketGenerationStrategy` / `IBracketCompletionStrategy`, one implementation per format, resolved
   via `IEnumerable<T>` + a `Format` property filtered by `tournament.Format` (chosen)
   - Pros: the minimum indirection that actually satisfies FR-014, since a new format adds a new class
     and one DI registration, never touching `BracketMutations`, `MatchMutations`, or either existing
     format's implementation; `BracketService`/`BracketCompletionService` keep their exact existing
     logic, just behind an interface, so Single Elimination's behavior is provably unchanged; each
     interface stays scoped to exactly the one seam each format actually needs (initial schedule
     generation; completion/champion detection), not a speculative do-everything format abstraction.
   - Cons: a new interface + DI-based dispatch where a single concrete service existed before is a real
     increase in indirection for a codebase that otherwise favors calling concrete services directly;
     two small interfaces to maintain instead of zero.

## Decision
Add `Tournament.Format` (`TournamentFormat` enum: `SingleElimination` = 0, default; `RoundRobin` = 1).
Introduce `IBracketGenerationStrategy` (generates a tournament's initial set of matches) and
`IBracketCompletionStrategy` (decides when a bracket is complete and who the champion is) in the
`Brackets` feature folder. `BracketService` and `BracketCompletionService` become the
`SingleElimination` implementations of these interfaces with their existing logic moved, not rewritten;
`RoundRobinBracketStrategy` and `RoundRobinCompletionStrategy` are the new `RoundRobin` implementations.
`BracketMutations.GenerateBracket` and `MatchMutations.Play`/`CorrectMatchResult` each resolve
`IEnumerable<TStrategy>.Single(s => s.Format == tournament.Format)` instead of depending on a concrete
service, so neither mutation file needs to change again when a third format is added; only a new
strategy class and its DI registration would.
