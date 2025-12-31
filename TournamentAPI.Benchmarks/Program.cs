using BenchmarkDotNet.Running;
using TournamentAPI.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes([typeof(ApiBenchmarks), typeof(SqlBenchmarks)]).Run(args);
    }
}