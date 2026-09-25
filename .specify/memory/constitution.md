<!--
Sync Impact Report
- Version change: (template, unratified) → 1.0.0
- Rationale for MAJOR: initial ratification — the file previously held only unfilled template
  placeholders, so this is the founding adoption, not an amendment.
- Modified principles: n/a (initial adoption)
- Added sections:
  - Core Principles: I. GraphQL-First, Type-Extension Architecture
  - Core Principles: II. Validation Without Exceptions
  - Core Principles: III. Minimal Abstraction (No Repository, No AutoMapper)
  - Core Principles: IV. Data Integrity via Optimistic Concurrency and Soft Delete
  - Core Principles: V. Explicit Types and Dependencies
  - Core Principles: VI. Documented Trade-offs
  - Technology & Structural Constraints
  - Development Workflow & Quality Gates
  - Governance
- Removed sections: none
- Deferred / TODO placeholders: none — all fields derived from CLAUDE.md and repo conventions
- Templates requiring follow-up: none checked in this run (out of scope per Scope Guard); consumers
  of this constitution (plan/spec/tasks templates) read it at runtime and were not modified here.
-->

# TournamentAPI Constitution

## Core Principles

### I. GraphQL-First, Type-Extension Architecture
The API surface MUST be defined implementation-first through HotChocolate v16. Queries and
mutations MUST be grouped per feature using `[ExtendObjectType]` on static partial classes,
never accumulated into one large `Query`/`Mutation` class. Domain entities MUST double as
GraphQL output types, with fields that should not be in the schema excluded via
`[GraphQLIgnore]` rather than mapped to separate DTOs.
Rationale: type-extension keeps each feature folder (`Tournaments/`, `Matches/`, `Users/`)
self-contained as the module boundary; exposing entities directly avoids a redundant mapping
layer, consistent with this project's rejection of mapping libraries.

### II. Validation Without Exceptions
Business-logic validation MUST be implemented as small static functions returning `IError?`
(`null` meaning valid), composed through `IResolverContext.TryReportError`. Exceptions MUST NOT
be used to signal business-logic flow. `DbUpdateException` handling is permitted only as the
fallback for concurrency conflicts that slip past explicit validation and optimistic-concurrency
checks.
Rationale: predictable, testable control flow for GraphQL resolvers, and cheap composition of
multiple validations without exception overhead on the hot path.

### III. Minimal Abstraction (No Repository, No AutoMapper)
The codebase MUST NOT introduce a repository pattern layer over EF Core. It MUST NOT use
AutoMapper or any other mapping library. Data access goes through EF Core's `DbContext`
directly; where mapping is unavoidable it is written by hand.
Rationale: at this project's current scale, a repository-over-EF-Core layer and mapping
libraries add indirection without a corresponding benefit. Introduce such abstractions only when
real, demonstrated duplication justifies them — not preemptively.

### IV. Data Integrity via Optimistic Concurrency and Soft Delete
Contested writes MUST use optimistic concurrency (`[Timestamp]` / version tokens), with
`DbUpdateException` handling as the fallback for whatever slips past it. Every domain entity
MUST implement soft delete via a global EF Core query filter; physical deletion of domain data
is not permitted through normal application code paths.
Rationale: tournament state (scores, brackets, participant rosters) is edited concurrently by
multiple actors, and tournament history has audit value that a hard delete would destroy.

### V. Explicit Types and Dependencies
Nullable reference types are enabled project-wide and MUST be treated as real signals:
nullability warnings MUST be fixed, not suppressed or ignored. Every new or edited C# file MUST
write out its `using` directives explicitly rather than relying on an IDE or linter to add them
later.
Rationale: in a GraphQL layer where resolver inputs are effectively attacker-controlled, explicit
nullability keeps null-safety intent visible in review instead of hidden behind `!` or warning
suppression; explicit usings keep a file's dependencies legible without tooling side effects.

### VI. Documented Trade-offs
Any decision with a genuine trade-off — concurrency strategy, storage or auth choice,
scheduling, rate limiting, a status-model change, and similar — MUST be recorded as an ADR in
`docs/adr/`, cross-referencing related prior ADRs the way existing ADRs in this repo already do.
Rationale: keeps the reasoning behind non-obvious architectural choices discoverable long after
the PR discussion that produced them has scrolled out of view.

## Technology & Structural Constraints

- Stack: .NET 9 / C# 13, HotChocolate v16 for GraphQL, EF Core 9 + SQL Server, ASP.NET Core
  Identity + JWT bearer authentication, Hangfire for scheduled jobs, OpenTelemetry + Serilog for
  tracing/metrics/logging.
- Architecture: modular monolith — one ASP.NET Core deployable, one shared database, feature
  folders under `TournamentAPI/` (e.g. `Tournaments/`, `Matches/`, `Users/`) as the module
  boundaries.
- File naming: one feature per folder; files named `<Feature><Concern>.cs`
  (`TournamentQueries`, `TournamentMutations`, `TournamentValidations`, `TournamentErrors`,
  `TournamentErrorCodes`, `TournamentDataLoaders`, `TournamentResolvers`,
  `TournamentLookupService`). Mutation inputs follow `<Verb><Feature>Input.cs`. Test classes
  mirror the class under test with a `Tests` suffix.
- Data loaders are static methods tagged `[DataLoader]` inside a `[DataLoaderGroup]` class.
- Options classes expose a `SectionName` const and use it in the matching `BindConfiguration`
  call.
- Rate limiting applies to `/graphql` only: a global concurrency limiter plus a per-IP token
  bucket.
- Mutation inputs are records; domain entities are mutable classes.

## Development Workflow & Quality Gates

- `dotnet build` MUST succeed, and both `TournamentAPI.UnitTests` and
  `TournamentAPI.IntegrationTests` MUST pass locally before work is considered done.
- Any query/mutation added, removed, or renamed, or any input/payload/error-code change, MUST be
  reflected in the Postman collection as part of the same change.
- Any decision with a real trade-off (see Principle VI) gets an ADR as part of the same change,
  not a follow-up.

## Governance

This constitution supersedes ad-hoc conventions where the two conflict. All PRs and reviews MUST
verify compliance with the Core Principles and the Development Workflow & Quality Gates above;
added complexity that deviates from Principle III MUST be justified in the PR description or an
ADR.

Amendments are made by editing this file directly, prepending an updated Sync Impact Report, and
following semantic versioning for the version line below:
- MAJOR: backward-incompatible governance or principle removals/redefinitions.
- MINOR: a new principle or section added, or materially expanded guidance.
- PATCH: clarifications, wording, or non-semantic refinements.

Dependent templates and commands (plan, spec, tasks) read this constitution at runtime; this
command does not modify them, so an amendment that changes expectations those templates encode
should be followed by a review of those templates in a separate change.

**Version**: 1.0.0 | **Ratified**: 2026-09-25 | **Last Amended**: 2026-09-25
