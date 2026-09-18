using Microsoft.EntityFrameworkCore;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Metrics;
using TournamentAPI.Tracing;

namespace TournamentAPI.Matches;

public class MatchCorrectionService(
    ILogger<MatchCorrectionService> logger,
    MatchCascadePositionCalculator cascadePositionCalculator,
    MatchMetrics matchMetrics)
{
    public async Task<Match> ApplyCorrectionAsync(
        ApplicationDbContext context,
        Match match,
        MatchStatus previousStatus,
        int? previousWinnerId,
        int previousPlayer1Id,
        int? previousPlayer2Id,
        int previousPlayer1Score,
        int previousPlayer2Score,
        int performedByUserId,
        Guid correlationId,
        CancellationToken token)
    {
        using var activity = TournamentActivitySource.Instance.StartActivity("MatchCorrectionService.ApplyCorrection");
        activity?.SetTag("match.id", match.Id);
        activity?.SetTag("correlation.id", correlationId.ToString());

        context.MatchCorrectionAudits.Add(BuildAuditRow(
            match,
            previousStatus, previousWinnerId, previousPlayer1Id, previousPlayer2Id, previousPlayer1Score, previousPlayer2Score,
            correlationId, triggeredByMatchId: null, performedByUserId, notes: null));

        if (previousWinnerId == match.WinnerId)
            return match;

        logger.LogInformation(
            "Match {MatchId} result corrected: winner {PreviousWinnerId} -> {NewWinnerId}, score {PreviousPlayer1Score}-{PreviousPlayer2Score} -> {NewPlayer1Score}-{NewPlayer2Score}",
            match.Id,
            previousWinnerId,
            match.WinnerId,
            previousPlayer1Score,
            previousPlayer2Score,
            match.Player1Score,
            match.Player2Score);

        return await PropagateAsync(context, match, previousWinnerId, performedByUserId, correlationId, token);
    }

    public async Task RecordIdempotentDuplicateAsync(
        ApplicationDbContext context,
        Match currentCommittedState,
        int performedByUserId,
        CancellationToken token)
    {
        context.MatchCorrectionAudits.Add(BuildAuditRow(
            currentCommittedState,
            currentCommittedState.Status, currentCommittedState.WinnerId,
            currentCommittedState.Player1Id, currentCommittedState.Player2Id,
            currentCommittedState.Player1Score, currentCommittedState.Player2Score,
            correlationId: Guid.NewGuid(),
            triggeredByMatchId: null,
            performedByUserId,
            notes: "Idempotent duplicate: retried request matched already-committed state."));

        logger.LogInformation(
            "Idempotent duplicate correction detected for match {MatchId}: retried request matched already-committed state",
            currentCommittedState.Id);

        await context.SaveChangesAsync(token);
    }

    private async Task<Match> PropagateAsync(
        ApplicationDbContext context,
        Match sourceMatch,
        int? sourceMatchPreviousWinnerId,
        int performedByUserId,
        Guid correlationId,
        CancellationToken token)
    {
        var upstreamMatch = sourceMatch;
        var upstreamPreviousWinnerId = sourceMatchPreviousWinnerId;

        while (true)
        {
            using var stepActivity = TournamentActivitySource.Instance.StartActivity("MatchCorrectionService.Propagate.Step");
            stepActivity?.SetTag("match.id", upstreamMatch.Id);
            stepActivity?.SetTag("round", upstreamMatch.Round);

            var currentRoundMatchIds = await context.Matches
                .Where(m => m.BracketId == upstreamMatch.BracketId && m.Round == upstreamMatch.Round)
                .OrderBy(m => m.Id)
                .Select(m => m.Id)
                .ToListAsync(token);

            var nextRoundMatches = await context.Matches
                .Where(m => m.BracketId == upstreamMatch.BracketId && m.Round == upstreamMatch.Round + 1)
                .OrderBy(m => m.Id)
                .ToListAsync(token);

            var nextRoundMatchIds = nextRoundMatches.Select(m => m.Id).ToList();

            var downstreamMatchId = cascadePositionCalculator.GetDownstreamMatchId(
                currentRoundMatchIds, nextRoundMatchIds, upstreamMatch.Id);

            if (downstreamMatchId is null)
                return upstreamMatch;

            stepActivity?.SetTag("downstream_match.id", downstreamMatchId);

            var downstream = nextRoundMatches.Single(m => m.Id == downstreamMatchId);

            var previousStatus = downstream.Status;
            var previousWinnerId = downstream.WinnerId;
            var previousPlayer1Id = downstream.Player1Id;
            var previousPlayer2Id = downstream.Player2Id;
            var previousPlayer1Score = downstream.Player1Score;
            var previousPlayer2Score = downstream.Player2Score;

            var newParticipantId = upstreamMatch.WinnerId!.Value;

            if (downstream.Player1Id == upstreamPreviousWinnerId)
                downstream.Player1Id = newParticipantId;
            else if (downstream.Player2Id == upstreamPreviousWinnerId)
                downstream.Player2Id = newParticipantId;

            if (downstream.Player2Id is null)
            {
                downstream.WinnerId = downstream.Player1Id;
                downstream.Status = MatchStatus.Played;

                context.MatchCorrectionAudits.Add(BuildAuditRow(
                    downstream,
                    previousStatus, previousWinnerId, previousPlayer1Id, previousPlayer2Id, previousPlayer1Score, previousPlayer2Score,
                    correlationId, upstreamMatch.Id, performedByUserId,
                    notes: "Auto-advanced bye after upstream correction."));

                upstreamMatch = downstream;
                upstreamPreviousWinnerId = previousWinnerId;
                continue;
            }

            if (downstream.Status == MatchStatus.Scheduled)
            {
                context.MatchCorrectionAudits.Add(BuildAuditRow(
                    downstream,
                    previousStatus, previousWinnerId, previousPlayer1Id, previousPlayer2Id, previousPlayer1Score, previousPlayer2Score,
                    correlationId, upstreamMatch.Id, performedByUserId,
                    notes: "Participant swapped after upstream correction; match not yet played."));

                return downstream;
            }

            downstream.Status = MatchStatus.NeedsReplay;
            matchMetrics.MatchNeedsReplay();

            context.MatchCorrectionAudits.Add(BuildAuditRow(
                downstream,
                previousStatus, previousWinnerId, previousPlayer1Id, previousPlayer2Id, previousPlayer1Score, previousPlayer2Score,
                correlationId, upstreamMatch.Id, performedByUserId,
                notes: "Invalidated by upstream correction."));

            return downstream;
        }
    }

    private static MatchCorrectionAudit BuildAuditRow(
        Match match,
        MatchStatus previousStatus,
        int? previousWinnerId,
        int previousPlayer1Id,
        int? previousPlayer2Id,
        int previousPlayer1Score,
        int previousPlayer2Score,
        Guid correlationId,
        int? triggeredByMatchId,
        int performedByUserId,
        string? notes)
        => new()
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            CorrelationId = correlationId,
            TriggeredByMatchId = triggeredByMatchId,
            PreviousStatus = previousStatus,
            NewStatus = match.Status,
            PreviousWinnerId = previousWinnerId,
            NewWinnerId = match.WinnerId,
            PreviousPlayer1Id = previousPlayer1Id,
            NewPlayer1Id = match.Player1Id,
            PreviousPlayer2Id = previousPlayer2Id,
            NewPlayer2Id = match.Player2Id,
            PreviousPlayer1Score = previousPlayer1Score,
            NewPlayer1Score = match.Player1Score,
            PreviousPlayer2Score = previousPlayer2Score,
            NewPlayer2Score = match.Player2Score,
            PerformedByUserId = performedByUserId,
            PerformedAtUtc = DateTime.UtcNow,
            Notes = notes
        };
}
