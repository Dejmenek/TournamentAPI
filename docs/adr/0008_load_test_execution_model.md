# Title
Load Test Execution Model

# Date
29/09/2026

## Status
Accepted

## Context
The load tests in `TournamentAPI.LoadTests` had four problems. They ran in parallel, so p95 assertions competed for CPU. They used a closed model (`KeepConstant`), where a slow server lowers the request rate and hides its own latency. Their limits were guesses, and no threshold was tied to a measured baseline. One test also exercised the wrong limiter. We needed to settle where the API runs during a load test, how load is generated, how tests are scheduled, and where the limits come from.

## Considered Options

### For where the API runs
1. In-process `WebApplicationFactory` with a Testcontainers SQL Server (chosen)
   - Pros: Same fixture style as the integration tests. No port or process management. Services can be overridden per test class (rate limiters, a delayed `IDbContextFactory` to hold concurrency permits, Hangfire removed).
   - Cons: The load generator and the server share one process, one CPU and one thread pool. There is no network or Kestrel in the path, so absolute numbers are optimistic and the generator can starve the server.
2. Real Kestrel host in a separate process
   - Pros: Closer to production. Generator and server no longer compete.
   - Cons: Port and process management, and the per-test service overrides above become much harder.

### For how load is generated
1. Open model with a fixed arrival rate, `Inject` and `RampingInject` (chosen)
   - Pros: The request rate does not depend on server speed, so a slowdown shows up as latency and queueing instead of fewer requests. This matches independent API clients.
   - Cons: Rates must be calibrated to capacity. In-flight requests are unbounded when the server is overloaded, and the in-process generator can fall behind the target rate.
2. Closed model with a fixed number of virtual users, `KeepConstant` and `RampingConstant`
   - Pros: Bounded concurrency, simple to reason about.
   - Cons: Slowdowns lower the request rate and flatter latency.

### For how tests are scheduled
1. Serialize all load tests (chosen)
   - Pros: One scenario at a time, so latency assertions are not distorted by another scenario's CPU use.
   - Cons: Longer total run time, and each test class starts its own SQL container.
2. Leave xUnit's default parallelism
   - Pros: Shorter wall-clock time.
   - Cons: Scenarios compete for CPU, which makes the p95 assertions flaky.

### For where the limits come from
1. Budgets measured from a baseline run, with headroom, kept in `LoadTestBudgets` (chosen)
   - Pros: Limits reflect real behaviour. Every threshold type reads from one file.
   - Cons: Numbers depend on the machine that took the baseline and need recalibrating when hardware or the query shape changes.
2. Limits from assumed targets (for example a fixed p95 of 500 ms)
   - Pros: No baseline run needed.
   - Cons: Either too loose to catch regressions or too tight to pass reliably.

## Decision
The API runs in-process, and load is generated as an open model with a fixed arrival rate. Tests run serialized, and every limit comes from a measured baseline in `LoadTestBudgets`.

The baseline was measured on a developer machine on 29/09/2026. At 50 req/s the list query had p95 55 ms, p99 104 ms, a maximum of 225 ms and a payload of 3,026 bytes. In the ramp, 100 req/s gave p95 325 ms, p99 488 ms and a maximum of 689 ms, and the run recovered to p95 34 ms after ramping back down. Budgets are set with headroom above those figures: for the list query, p95 150 ms, p99 300 ms and a maximum of 1,000 ms. Ramp windows have their own limits, because peak load is expected to run slower than steady load.

Numbers from the in-process host are comparable between runs on the same machine. They are not production latency figures.

## Related ADRs
- ADR #0001 Rate Limiting Algorithm for API Requests: the concurrency limiter and token bucket tests exercise the chained global limiter that ADR decided on. Running them showed the per-IP limiter was attached as an endpoint policy before routing and never took effect, so it was moved into the global limiter.
