using System.Diagnostics;

namespace TournamentAPI.Tracing;

public static class TournamentActivitySource
{
    public const string Name = "TournamentAPI";

    public static readonly ActivitySource Instance = new(Name);
}
