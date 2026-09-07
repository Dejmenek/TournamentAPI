using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TournamentAPI.Data.Models;
using TournamentAPI.Tournaments;

namespace TournamentAPI.IntegrationTests.Jobs;

public class TournamentAutoCloseJobTests : BaseIntegrationTest
{
    public TournamentAutoCloseJobTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RunAsync_ClosesOpenTournamentsPastStartDate_AndLeavesOthersUntouched()
    {
        // Arrange
        var pastStartDateTournamentId = 1;
        var futureStartDateTournamentId = 2;

        var tournamentToClose = await DbContext.Tournaments.FirstAsync(t => t.Id == pastStartDateTournamentId);
        tournamentToClose.StartDate = DateTime.UtcNow.AddMinutes(-10);
        await DbContext.SaveChangesAsync();

        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TournamentAutoCloseJob>();

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert
        var closedTournament = await DbContext.Tournaments
            .AsNoTracking()
            .FirstAsync(t => t.Id == pastStartDateTournamentId);
        Assert.Equal(TournamentStatus.Closed, closedTournament.Status);

        var untouchedTournament = await DbContext.Tournaments
            .AsNoTracking()
            .FirstAsync(t => t.Id == futureStartDateTournamentId);
        Assert.Equal(TournamentStatus.Open, untouchedTournament.Status);
    }

    [Fact]
    public async Task RunAsync_WhenNothingIsDue_IsANoOp()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TournamentAutoCloseJob>();

        var statusesBefore = await DbContext.Tournaments
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Status);

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert
        var statusesAfter = await DbContext.Tournaments
            .AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.Status);

        Assert.Equal(statusesBefore, statusesAfter);
    }
}
