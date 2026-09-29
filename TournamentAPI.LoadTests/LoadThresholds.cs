using System.Linq.Expressions;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using Xunit;

namespace TournamentAPI.LoadTests;

internal sealed class LoadThreshold
{
    private readonly string? _stepName;
    private readonly Expression<Func<ScenarioStats, bool>>? _scenarioCheck;
    private readonly Expression<Func<StepStats, bool>>? _stepCheck;
    private readonly Func<ScenarioStats, bool>? _compiledScenarioCheck;
    private readonly Func<StepStats, bool>? _compiledStepCheck;

    private LoadThreshold(
        string name,
        string? stepName,
        Expression<Func<ScenarioStats, bool>>? scenarioCheck,
        Expression<Func<StepStats, bool>>? stepCheck)
    {
        Name = name;
        _stepName = stepName;
        _scenarioCheck = scenarioCheck;
        _stepCheck = stepCheck;
        _compiledScenarioCheck = scenarioCheck?.Compile();
        _compiledStepCheck = stepCheck?.Compile();
    }

    public string Name { get; }

    public static LoadThreshold ForScenario(string name, Expression<Func<ScenarioStats, bool>> check)
        => new(name, null, check, null);

    public static LoadThreshold ForStep(string name, string stepName, Expression<Func<StepStats, bool>> check)
        => new(name, stepName, null, check);

    public bool IsSatisfiedBy(ScenarioStats stats)
    {
        return _stepName is null
            ? _compiledScenarioCheck!(stats)
            : _compiledStepCheck!(stats.StepStats.Get(_stepName));
    }

    public Threshold ToNBomberThreshold()
    {
        return _stepName is null
            ? Threshold.Create(_scenarioCheck!)
            : Threshold.Create(_stepName, _stepCheck!);
    }
}

internal static class LoadThresholds
{
    public static LoadThreshold ErrorBudget(double maxFailPercent)
        => LoadThreshold.ForScenario(
            $"error budget: fail rate <= {maxFailPercent}%",
            s => s.Fail.Request.Percent <= maxFailPercent);

    public static LoadThreshold TailLatency(string stepName, double p95Ms, double p99Ms, double maxMs)
        => LoadThreshold.ForStep(
            $"tail latency [{stepName}]: p95 <= {p95Ms}ms, p99 <= {p99Ms}ms, max <= {maxMs}ms",
            stepName,
            s => s.Ok.Latency.Percent95 <= p95Ms
                && s.Ok.Latency.Percent99 <= p99Ms
                && s.Ok.Latency.MaxMs <= maxMs);

    public static LoadThreshold StatusMix(string okCode, double minOkPercent, params string[] allowedFailCodes)
        => LoadThreshold.ForScenario(
            $"status mix: {okCode} >= {minOkPercent}% of requests, failures only in [{string.Join(", ", allowedFailCodes)}]",
            s => IsStatusMixSatisfied(s, okCode, minOkPercent, allowedFailCodes));

    public static LoadThreshold PayloadGuard(string stepName, double minMeanBytes, double maxMeanBytes)
        => LoadThreshold.ForStep(
            $"payload guard [{stepName}]: mean response between {minMeanBytes} and {maxMeanBytes} bytes",
            stepName,
            s => s.Ok.DataTransfer.MeanBytes >= minMeanBytes && s.Ok.DataTransfer.MeanBytes <= maxMeanBytes);

    public static LoadThreshold StepHealth(string stepName, double maxFailPercent)
        => LoadThreshold.ForStep(
            $"step health [{stepName}]: fail rate <= {maxFailPercent}%",
            stepName,
            s => s.Fail.Request.Percent <= maxFailPercent);

    public static bool IsStatusMixSatisfied(
        ScenarioStats stats,
        string okCode,
        double minOkPercent,
        string[] allowedFailCodes)
    {
        var total = stats.Ok.Request.Count + stats.Fail.Request.Count;
        if (total == 0)
        {
            return false;
        }

        var okCount = stats.Ok.StatusCodes.Find(okCode)?.Count ?? 0;
        var hasUnexpectedFailure = stats.Fail.StatusCodes.Any(c => !allowedFailCodes.Contains(c.StatusCode));

        return okCount * 100.0 / total >= minOkPercent && !hasUnexpectedFailure;
    }
}

internal static class LoadTestRun
{
    public static ScenarioStats Execute(ScenarioProps scenario, params LoadThreshold[] thresholds)
    {
        var withThresholds = thresholds.Length == 0
            ? scenario
            : scenario.WithThresholds(thresholds.Select(t => t.ToNBomberThreshold()).ToArray());

        var result = NBomberRunner
            .RegisterScenarios(withThresholds)
            .Run();

        var stats = result.ScenarioStats.Get(scenario.ScenarioName);
        AssertPassed(stats, thresholds);

        return stats;
    }

    public static void AssertPassed(ScenarioStats stats, IEnumerable<LoadThreshold> thresholds)
    {
        var failed = thresholds.Where(t => !t.IsSatisfiedBy(stats)).Select(t => t.Name).ToList();

        if (failed.Count > 0)
        {
            Assert.Fail($"Load thresholds failed for '{stats.ScenarioName}':{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", failed)}");
        }
    }
}
