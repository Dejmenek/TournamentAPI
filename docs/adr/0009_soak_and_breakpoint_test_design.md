# Title
Soak and Breakpoint Test Design

# Date
29/09/2026

## Status
Accepted

## Context
The load tests covered steady traffic, a phased ramp and writes under contention, but nothing checked a sudden burst, a long run, or where the API stops coping. Two design questions came up. The API runs in-process with the limiters turned off, so overload shows up as queueing and slow responses, not as errors. And a soak run has to show drift over time without turning into a set of unrelated short runs.

## Considered Options

### For how the breakpoint is found
1. Step through fixed rates in one run and read the breakpoint from per step p95 after the run (chosen)
   - Pros: Works when overload appears as latency instead of errors. Every rate keeps its own stats, so a failure message can list p95 for each rate and show the whole curve. A safety abort on a very high error rate still stops a runaway run.
   - Cons: The run always goes through every rate, even after the API has clearly broken. A noisy step ends the search early, so the breakpoint can read low.
2. Abort the run as soon as the error budget breaks
   - Pros: Stops early and spends no time above the break.
   - Cons: The in-process host queues under overload instead of failing, so the run may never abort and would report no breakpoint at all.

### For how the soak is run
1. One 12 minute run with a warm up phase and six named two minute segments, plus managed memory sampled after forced collections (chosen)
   - Pros: One host and one container live for the whole run, so a leak or slow degradation shows up as a trend between segments. Naming steps by segment reuses the phase pattern from the ramp test.
   - Cons: The run takes about 12.5 minutes, so it stays manual and carries its own trait (Category=Soak). Each step gets a check per segment, and a busy machine can fail a single segment. The memory check only sees the managed heap of the test process, which includes the API, and the 150 MB growth limit has not been calibrated against a measured run.
2. Separate short runs, one per segment
   - Pros: Every segment starts from a clean state.
   - Cons: A fresh host per run hides exactly the drift a soak exists to find.

## Decision
The breakpoint test steps a heavy query through 25, 50, 75, 100, 125 and 150 requests per second, 15 seconds each after a warm up. The breakpoint is the highest rate whose p95 stays under 800 ms with no failures. On the development machine it was 125 requests per second in two runs, and 150 broke the ceiling both times (p95 1,425 ms and 4,116 ms). The test asserts a floor of 50 requests per second.

The spike test uses the same heavy query. A cheap list query showed a p95 of 22 ms even at 200 requests per second, which proves nothing. The heavy query goes from 25 to 150 requests per second for 10 seconds and back. The burst p95 reached about 1.4 s, and p95 returned to baseline after a 15 second drain. Budgets come from the worst of three runs with the same headroom as the other tests.

The soak runs the mixed browsing load at the existing rates and compares the last segment with the first. Soak and breakpoint tests carry Category=Soak only, so `Category=Load` never picks them up.

## Related ADRs
- ADR #0008 Load Test Execution Model: keeps the in-process host, the open arrival model, serialized tests and budgets measured from a baseline. This ADR adds phased spike, soak and breakpoint runs on top of it.
