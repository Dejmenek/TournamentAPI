using BenchmarkDotNet.Running;
using TournamentAPI.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run<ApiBenchmarks>();
    }
}