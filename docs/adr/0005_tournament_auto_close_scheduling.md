# Title
Scheduled Auto-Close of Tournaments Past StartDate

# Date
07/09/2026

## Status
Accepted

## Context
`Tournament.Status` (`Open`/`Closed`) is a purely manual flag; nothing transitions it based on
`StartDate`. A tournament stays `Open` past its `StartDate` unless an owner explicitly calls
`UpdateTournament`, and `JoinTournament`/`AddParticipant` gate purely on `Status`, so users can keep
joining a tournament that has already started.

We need a mechanism to close these tournaments automatically, and a way to make the closure take
effect immediately for API consumers rather than only after the next scheduled run.

## Considered Options
1. `BackgroundService` + `PeriodicTimer` (built into ASP.NET Core)
   - Pros: No new package.
   - Cons: In-process only. State doesn't survive a restart, there's no retry/failure visibility,
     and multiple API instances each run an independent, uncoordinated timer.
2. Hangfire with SQL Server storage, no dashboard (chosen)
   - Pros: Recurring-job state survives restarts, coordinates automatically across instances, gives
     built-in retry (`[AutomaticRetry]`) and overlap protection (`[DisableConcurrentExecution]`),
     and reuses the existing SQL Server database. The dashboard is optional and simply not wired up.
   - Cons: New dependency (`Hangfire.AspNetCore`, `Hangfire.SqlServer`) and its own schema in the
     database; still only *eventually* consistent with `StartDate`, bounded by the polling interval.
3. A lazily-evaluated check alone, no background job
   - Pros: Immediate consistency everywhere the check is applied.
   - Cons: The stored `Status` column never catches up, misleading anything that reads it directly
     (reports, admin tooling, direct DB queries).

## Decision
We use Hangfire with SQL Server storage (`ConnectionStrings:DefaultConnection`) for a recurring
`TournamentAutoCloseJob` that runs every 5 minutes (`Cron.MinuteInterval(5)`), closing `Open`
tournaments whose `StartDate` has passed via a single `ExecuteUpdateAsync` statement. No dashboard is
exposed: only `AddHangfireServer()` is registered.

Since the job only runs every 5 minutes, we pair it with a lazily-evaluated check
(`Tournament.IsActive(DateTime utcNow)`) on the paths that decide whether a tournament is still open
for registration: `JoinTournament`, `AddParticipant`, `UpdateTournament`'s `MaxParticipants` check and
reopen guard, plus a new `isActive` GraphQL field. A tournament is treated as inactive for
registration the instant `StartDate` passes, regardless of what `Status` says. The job's role is just
to make the stored value eventually match reality, not to gate registration on its own.

Bracket generation, round updates, and match `Play` keep checking the literal `Status == Closed`
instead. Those answer a different question, "has the participant list actually been finalized",
not "has `StartDate` passed", and letting a past-`StartDate`-but-still-`Open` tournament satisfy
them would let a bracket be built or a round advanced before registration was ever formally closed.

5 minutes balances closure lag (a tournament can report `Status: Open` for up to 5 minutes after
`StartDate`, even though `isActive` and the mutation checks already treat it as closed) against job
overhead, which is negligible for a lightweight `UPDATE ... WHERE` sweep.
