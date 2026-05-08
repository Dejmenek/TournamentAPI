using System.Diagnostics.Metrics;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Metrics;

public class TournamentMetrics : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Counter<int> _tournamentsCreated;

    public TournamentMetrics(IMeterFactory meterFactory, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;

        var meter = meterFactory.Create(MetricConstants.TournamentMeterName);
        _tournamentsCreated = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_created", description: "Number of tournaments created");

        _ = meter.CreateObservableGauge(
            "tournamentapi.tournaments.active_tournaments",
            () => GetActiveTournamentsCount(),
            description: "Number of currently active tournaments");
    }

    public void IncrementTournamentsCreated()
    {
        _tournamentsCreated.Add(1);
    }

    private int GetActiveTournamentsCount()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return context.Tournaments.Count(t => t.Status == TournamentStatus.Open);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
