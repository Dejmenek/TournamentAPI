using NBomber.Contracts;
using NBomber.CSharp;

namespace TournamentAPI.LoadTests;

internal sealed record LoadPhase(string Name, int RatePerSecond, TimeSpan Duration);

internal static class LoadPhases
{
    public static LoadSimulation[] ToSimulations(IEnumerable<LoadPhase> phases)
    {
        return phases
            .Select(phase => Simulation.Inject(phase.RatePerSecond, TimeSpan.FromSeconds(1), phase.Duration))
            .ToArray();
    }

    public static string NameAt(IReadOnlyList<LoadPhase> phases, TimeSpan elapsed)
    {
        var phaseEnd = TimeSpan.Zero;

        foreach (var phase in phases)
        {
            phaseEnd += phase.Duration;
            if (elapsed < phaseEnd)
            {
                return phase.Name;
            }
        }

        return phases[^1].Name;
    }
}
