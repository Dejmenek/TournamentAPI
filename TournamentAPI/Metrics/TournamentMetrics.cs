using System.Diagnostics.Metrics;

namespace TournamentAPI.Metrics;

public class TournamentMetrics : IHostedService
{
    private readonly Counter<int> _tournamentsCreated;

    public TournamentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MetricConstants.TournamentMeterName);
        _tournamentsCreated = meter.CreateCounter<int>("tournamentapi.tournaments.tournaments_created", description: "Number of tournaments created");
    }

    public void IncrementTournamentsCreated()
    {
        _tournamentsCreated.Add(1);
    }
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
