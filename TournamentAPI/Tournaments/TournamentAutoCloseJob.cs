using Hangfire;
using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Metrics;
using TournamentAPI.Tracing;

namespace TournamentAPI.Tournaments;

[DisableConcurrentExecution(timeoutInSeconds: 30)]
[AutomaticRetry(Attempts = 3)]
public class TournamentAutoCloseJob
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TournamentMetrics _tournamentMetrics;
    private readonly ILogger<TournamentAutoCloseJob> _logger;

    public TournamentAutoCloseJob(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TournamentMetrics tournamentMetrics,
        ILogger<TournamentAutoCloseJob> logger)
    {
        _contextFactory = contextFactory;
        _tournamentMetrics = tournamentMetrics;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var activity = TournamentActivitySource.Instance.StartActivity("TournamentAutoCloseJob.Run");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var closedCount = await context.Tournaments
            .Where(t => t.Status == TournamentStatus.Open && t.StartDate <= now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(t => t.Status, TournamentStatus.Closed),
                cancellationToken);

        activity?.SetTag("tournament.auto_close.count", closedCount);

        if (closedCount == 0)
            return;

        for (var i = 0; i < closedCount; i++)
            _tournamentMetrics.TournamentClosed();

        _tournamentMetrics.IncrementTournamentsClosed("auto", closedCount);

        _logger.LogInformation("Auto-closed {Count} tournament(s) past StartDate.", closedCount);
    }
}
