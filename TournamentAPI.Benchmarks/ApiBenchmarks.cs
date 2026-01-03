using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Shared.Helpers;
using TournamentAPI.Shared.Models;

namespace TournamentAPI.Benchmarks;

[MemoryDiagnoser]
public class ApiBenchmarks : IAsyncDisposable
{
    private BenchmarkWebAppFactory? _factory;
    private TestClient? _client;

    [Params(DataSize.Small, DataSize.Medium, DataSize.Large)]
    public DataSize Size { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _factory = new BenchmarkWebAppFactory(Size);
        await SeedDatabaseAsync();

        var httpClient = _factory.CreateClient();
        _client = new TestClient(httpClient);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        _client?.Dispose();
        await (_factory?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await BenchmarkDatabaseSeeder.SeedAsync(context, userManager, Size);
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentsResponse>> GetAllTournaments()
    {
        return await _client!.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithTotalCount);
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentsResponse>> GetAllTournamentsWithParticipants()
    {
        return await _client!.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithParticipants);
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentsResponse>> GetAllTournamentsWithBracketAndMatches()
    {
        return await _client!.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithBracketAndMatches);
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentsResponse>> GetAllTournamentsWithOwner()
    {
        return await _client!.ExecuteQueryAsync<TournamentsResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetAllWithOwner);
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentByIdResponse>> GetTournamentById()
    {
        return await _client!.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetById,
            new { id = 1 });
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentByIdResponse>> GetTournamentByIdWithParticipants()
    {
        return await _client!.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithParticipants,
            new { id = 1 });
    }

    [Benchmark]
    public async Task<GraphQLResponse<TournamentByIdResponse>> GetTournamentByIdWithBracketAndMatches()
    {
        return await _client!.ExecuteQueryAsync<TournamentByIdResponse>(
            Shared.QueryExamples.Queries.Tournaments.GetByIdWithBracketAndMatches,
            new { id = 1 });
    }

    public async ValueTask DisposeAsync()
    {
        await GlobalCleanup();
    }
}
