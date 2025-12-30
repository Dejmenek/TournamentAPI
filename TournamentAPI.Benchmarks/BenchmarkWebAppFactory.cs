extern alias TournamentApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace TournamentAPI.Benchmarks;

internal class BenchmarkWebAppFactory : WebApplicationFactory<TournamentApi::Program>
{
    private readonly DataSize _dataSize;

    public BenchmarkWebAppFactory(DataSize dataSize)
    {
        _dataSize = dataSize;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Benchmark");
    }

    public DataSize GetDataSize() => _dataSize;
}
