extern alias TournamentApi;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentApi::TournamentAPI.Data;
using TournamentApi::TournamentAPI.Data.Models;
using TournamentAPI.Shared.Helpers;

namespace TournamentAPI.Benchmarks;

[MemoryDiagnoser]
public class SqlBenchmarks : IAsyncDisposable
{
    private BenchmarkWebAppFactory? _factory;
    private TestClient? _client;
    private int _secondPageCursor;
    private int _deepPageOffset;
    private int _ownerId;

    [Params(DataSize.Small, DataSize.Medium, DataSize.Large)]
    public DataSize Size { get; set; }

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _factory = new BenchmarkWebAppFactory(Size);
        await SeedDatabaseAsync();

        var httpClient = _factory.CreateClient();
        _client = new TestClient(httpClient);

        (_secondPageCursor, _deepPageOffset, _ownerId) = Size switch
        {
            DataSize.Small => (3, 0, 1),
            DataSize.Medium => (10, 20, 1),
            DataSize.Large => (10, 50, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(Size))
        };
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
    public async Task<List<Tournament>> QueryTournamentsByStatus()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Status == TournamentStatus.Open)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsByOwnerId()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.OwnerId == _ownerId)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsByStartDate()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var dateThreshold = DateTime.UtcNow;
        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.StartDate > dateThreshold)
            .OrderBy(t => t.StartDate)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsWithSoftDeleteFilter()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsComposite_StatusAndIsDeleted()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Status == TournamentStatus.Open && !t.IsDeleted)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<TournamentParticipant>> QueryParticipantsByTournamentId()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.TournamentId == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<TournamentParticipant>> QueryParticipantsByParticipantId()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.TournamentParticipants
            .AsNoTracking()
            .Where(tp => tp.ParticipantId == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<TournamentParticipant?> QueryParticipantComposite_TournamentAndParticipant()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.TournamentParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(tp => tp.TournamentId == 1 && tp.ParticipantId == 2);
    }

    [Benchmark]
    public async Task<List<TournamentParticipant>> QueryParticipantsWithSoftDeleteFilter()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.TournamentParticipants
            .AsNoTracking()
            .Where(tp => !tp.IsDeleted)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Match>> QueryMatchesByBracketId()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Matches
            .AsNoTracking()
            .Where(m => m.BracketId == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Match>> QueryMatchesByBracketAndRound()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Matches
            .AsNoTracking()
            .Where(m => m.BracketId == 1 && m.Round == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Match>> QueryMatchesByPlayer()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Matches
            .AsNoTracking()
            .Where(m => m.Player1Id == 1 || m.Player2Id == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Match>> QueryMatchesByWinner()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Matches
            .AsNoTracking()
            .Where(m => m.WinnerId == 1)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<Bracket?> QueryBracketByTournamentId()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Brackets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TournamentId == 1);
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsWithParticipantsInclude()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Include(t => t.Participants)
            .Where(t => !t.IsDeleted)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsWithBracketAndMatchesInclude()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Include(t => t.Bracket)
                .ThenInclude(b => b!.Matches)
            .Where(t => t.Status == TournamentStatus.Closed)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsCursorPaging_FirstPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsCursorPaging_SecondPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Id > _secondPageCursor)
            .OrderBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsCursorPaging_WithSortingFirstPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsCursorPaging_WithFilteringFirstPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Status == TournamentStatus.Open)
            .OrderBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsCursorPaging_WithFilteringAndSortingFirstPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Status == TournamentStatus.Open)
            .OrderByDescending(t => t.StartDate)
            .ThenBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsOffsetPaging_FirstPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsOffsetPaging_SecondPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip(10)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsOffsetPaging_DeepPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Skip(_deepPageOffset)
            .Take(10)
            .ToListAsync();
    }

    [Benchmark]
    public async Task<List<Tournament>> QueryTournamentsOffsetPaging_WithFilteringSecondPage()
    {
        using var scope = _factory!.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Tournaments
            .AsNoTracking()
            .Where(t => t.Status == TournamentStatus.Open)
            .OrderBy(t => t.Id)
            .Skip(10)
            .Take(10)
            .ToListAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await GlobalCleanup();
    }
}
