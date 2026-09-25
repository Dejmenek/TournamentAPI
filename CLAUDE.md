## Project overview

TournamentAPI is a GraphQL API for running tournaments: creating them, managing participants, and generating brackets

## Project structure

| Project | Purpose |
|---|---|
| `TournamentAPI` | Main ASP.NET Core web API |
| `TournamentAPI.Shared` | Shared models and test helpers |
| `TournamentAPI.IntegrationTests` | xUnit + TestContainers integration tests |
| `TournamentAPI.LoadTests` | NBomber load/stress tests |
| `TournamentAPI.Benchmarks` | BenchmarkDotNet performance benchmarks |
| `TournamentAPI.UnitTests` | Unit tests for services and utilities |

## Tech stack

- .NET 9 / C# 13
- HotChocolate v16 for GraphQL
- EF Core 9 + SQL Server
- ASP.NET Core Identity + JWT bearer authentication
- Hangfire for scheduled jobs
- OpenTelemetry + Serilog for tracing, metrics, and logging
- xUnit + TestContainers (integration), NBomber (load), BenchmarkDotNet (benchmarks)

## Architecture

Modular monolith: one ASP.NET Core deployable, one shared database, feature folders as the module boundaries.

- Rate limiting - Applied to `/graphql` only: a global concurrency limiter, plus a per-IP token bucket

## File naming convention

- One feature per folder under `TournamentAPI/` (`Tournaments/`, `Matches/`, `Users/`)
- Files are named `<Feature><Concern>.cs`: `TournamentQueries`, `TournamentMutations`, `TournamentValidations`, `TournamentErrors`, `TournamentErrorCodes`, `TournamentDataLoaders`, `TournamentResolvers`, `TournamentLookupService`
- Mutation inputs follow `<Verb><Feature>Input.cs`, e.g. `CreateTournamentInput`, `UpdateTournamentInput`
- Test classes mirror the class under test with a `Tests` suffix, e.g. `TournamentQueryTests`

## Code conventions

- Nullable reference types and implicit usings are on everywhere; treat nullability warnings as real problems, not noise
- Queries and mutations are grouped per feature using `[ExtendObjectType]` on static partial classes. Domain entities double as GraphQL output types; fields that shouldn't be in the schema are marked `[GraphQLIgnore]`
- Validation methods return `IError?`, with `null` meaning valid, instead of throwing
- Mutation inputs are records; domain entities are mutable classes
- Data loaders are static methods tagged `[DataLoader]` inside a `[DataLoaderGroup]` class
- Write out every `using` directive explicitly rather than relying on a linter to add it later
- Options classes expose a `SectionName` const and use it in the matching `BindConfiguration` call

## Patterns we use

- Implementation-first HotChocolate over code-first
- Type-extension GraphQL layer (`[ExtendObjectType]`) instead of one large `Query`/`Mutation` class
- Entities exposed directly as GraphQL types, trimmed down with `[GraphQLIgnore]`
- Validation as small static functions returning `IError?`, composed through `IResolverContext.TryReportError`
- Optimistic concurrency (`[Timestamp]`, version tokens) for contested writes, with `DbUpdateException` handling as the fallback for whatever slips past it
- Soft delete with global query filters on every domain entity
- An ADR in `docs/adr/` for any decision with a real trade-off

## Patterns we don't use

- Repository pattern
- AutoMapper, or any mapping library
- Exceptions for business-logic flow

## Common commands

```bash
# Build
dotnet build

# Run API (development)
dotnet run --project TournamentAPI

# Run all unit tests
dotnet test TournamentAPI.UnitTests

# Run all integration tests
dotnet test TournamentAPI.IntegrationTests

# Run tests by class name
dotnet test TournamentAPI.IntegrationTests --filter "TournamentQueryTests"

# Run a single test method
dotnet test TournamentAPI.IntegrationTests --filter "FullyQualifiedName~TournamentQueryTests.GetTournaments_ReturnsAllTournamentsWithTotalCount"

# Run load tests
dotnet test TournamentAPI.LoadTests

# Run benchmarks (must be Release)
dotnet run --project TournamentAPI.Benchmarks --configuration Release
```

## Definition of done

- `dotnet build` succeeds, and both `TournamentAPI.UnitTests` and `TournamentAPI.IntegrationTests` pass locally
- Any query/mutation added, removed, or renamed, or any input/payload/error-code change, is reflected in both the Postman collection as part of the same change
- Any decision with a real trade-off gets an ADR