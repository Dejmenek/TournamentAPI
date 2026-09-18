using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Brackets;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Tournaments;
using TournamentAPI.Tracing;

namespace TournamentAPI.Matches;

[MutationType]
public static partial class MatchMutations
{
    [Authorize]
    public static async Task<bool?> Play(
        int matchId,
        int winnerId,
        int player1Score,
        int player2Score,
        ClaimsPrincipal userClaims,
        IResolverContext resolverContext,
        ApplicationDbContext context,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchExists(match, matchId)))
            return null;

        var tournament = match!.Bracket.Tournament;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, tournament.Id)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateTournamentIsClosed(tournament)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchNotPlayed(match)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateWinnerIsParticipant(match, winnerId)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateScoresAreNonNegative(match.Id, player1Score, player2Score)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateWinnerHasHigherScore(match, winnerId, player1Score, player2Score)))
            return null;

        var isReplay = match.Status == MatchStatus.NeedsReplay;
        var previousStatus = match.Status;
        var previousWinnerId = match.WinnerId;
        var previousPlayer1Id = match.Player1Id;
        var previousPlayer2Id = match.Player2Id;
        var previousPlayer1Score = match.Player1Score;
        var previousPlayer2Score = match.Player2Score;

        match.WinnerId = winnerId;
        match.Player1Score = player1Score;
        match.Player2Score = player2Score;
        match.Status = MatchStatus.Played;

        try
        {
            var frontierMatch = match;

            if (isReplay)
            {
                frontierMatch = await MatchCorrectionService.ApplyCorrectionAsync(
                    context,
                    match,
                    previousStatus,
                    previousWinnerId,
                    previousPlayer1Id,
                    previousPlayer2Id,
                    previousPlayer1Score,
                    previousPlayer2Score,
                    userId,
                    Guid.NewGuid(),
                    token);
            }

            await BracketCompletionService.SyncChampionAsync(context, tournament, match.BracketId, frontierMatch.Round, token);

            await context.SaveChangesAsync(token);

            return true;
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(MatchErrors.MatchAlreadyPlayed(matchId));
            return null;
        }
    }

    [Authorize]
    public static async Task<bool?> CorrectMatchResult(
        int matchId,
        int winnerId,
        int player1Score,
        int player2Score,
        string version,
        ClaimsPrincipal userClaims,
        IResolverContext resolverContext,
        ApplicationDbContext context,
        CancellationToken token)
    {
        using var activity = TournamentActivitySource.Instance.StartActivity("Match.CorrectMatchResult");
        activity?.SetTag("match.id", matchId);

        var logger = loggerFactory.CreateLogger(typeof(MatchMutations).FullName!);

        var userId = userClaims.GetUserId();

        var match = await context.Matches
            .Include(m => m.Bracket)
                .ThenInclude(b => b.Tournament)
            .FirstOrDefaultAsync(m => m.Id == matchId, token);

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchExists(match, matchId)))
            return null;

        var tournament = match!.Bracket.Tournament;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, tournament.Id)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateTournamentIsClosed(tournament)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchNotScheduled(match)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateMatchNotNeedsReplay(match)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateWinnerIsParticipant(match, winnerId)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateScoresAreNonNegative(match.Id, player1Score, player2Score)))
            return null;

        if (resolverContext.TryReportError(MatchValidations.ValidateWinnerHasHigherScore(match, winnerId, player1Score, player2Score)))
            return null;

        if (!MatchVersionCodec.TryDecode(version, out var decodedRowVersion))
        {
            resolverContext.ReportError(MatchErrors.InvalidVersionToken(matchId));
            return null;
        }

        context.Entry(match).Property(m => m.RowVersion).OriginalValue = decodedRowVersion;

        var previousStatus = match.Status;
        var previousWinnerId = match.WinnerId;
        var previousPlayer1Id = match.Player1Id;
        var previousPlayer2Id = match.Player2Id;
        var previousPlayer1Score = match.Player1Score;
        var previousPlayer2Score = match.Player2Score;

        match.WinnerId = winnerId;
        match.Player1Score = player1Score;
        match.Player2Score = player2Score;
        match.Status = MatchStatus.Played;

        try
        {
            var frontierMatch = await MatchCorrectionService.ApplyCorrectionAsync(
                context,
                match,
                previousStatus,
                previousWinnerId,
                previousPlayer1Id,
                previousPlayer2Id,
                previousPlayer1Score,
                previousPlayer2Score,
                userId,
                Guid.NewGuid(),
                token);

            await BracketCompletionService.SyncChampionAsync(context, tournament, match.BracketId, frontierMatch.Round, token);

            await context.SaveChangesAsync(token);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();

            var currentState = await context.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == matchId, token);

            if (currentState is not null
                && currentState.WinnerId == winnerId
                && currentState.Player1Score == player1Score
                && currentState.Player2Score == player2Score)
            {
                await MatchCorrectionService.RecordIdempotentDuplicateAsync(context, currentState, userId, token);
                return true;
            }

            resolverContext.ReportError(MatchErrors.MatchVersionConflict(matchId));
            return null;
        }
        catch (DbUpdateException)
        {
            resolverContext.ReportError(MatchErrors.MatchCorrectionFailed(matchId));
            return null;
        }
    }
}
