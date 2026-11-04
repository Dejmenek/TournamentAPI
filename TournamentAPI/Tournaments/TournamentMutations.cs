using GreenDonut.Data;
using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TournamentAPI.Brackets;
using TournamentAPI.Data;
using TournamentAPI.Data.Models;
using TournamentAPI.Extensions;
using TournamentAPI.Metrics;
using TournamentAPI.Payments;

namespace TournamentAPI.Tournaments;

[MutationType]
public static partial class TournamentMutations
{
    [Authorize]
    public static async Task<bool?> JoinTournament(
        int tournamentId,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        ParticipantMetrics participantMetrics,
        CancellationToken token)
    {
        using var _ = resolverContext.PushEntityContext("Tournament", tournamentId);

        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, tournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(PaymentValidations.ValidateTournamentHasNoEntryFee(tournament.Id, tournament.EntryFeeAmount)))
        {
            participantMetrics.JoinAttempt("checkout_required");
            return null;
        }

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotClosed(tournament, DateTime.UtcNow)))
        {
            participantMetrics.JoinAttempt("closed");
            return null;
        }

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNotFull(tournament)))
        {
            participantMetrics.JoinAttempt("full");
            return null;
        }

        if (resolverContext.TryReportError(TournamentValidations.ValidateUserNotAlreadyParticipant(tournament, userId)))
        {
            participantMetrics.JoinAttempt("already_joined");
            return null;
        }

        var participant = new TournamentParticipant
        {
            TournamentId = tournamentId,
            ParticipantId = userId,
            SlotNumber = tournament.Participants.Count + 1
        };

        context.TournamentParticipants.Add(participant);
        try
        {
            await context.SaveChangesAsync(token);
            participantMetrics.JoinAttempt("success");
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation(TournamentParticipant.SlotNumberUniqueIndexName))
        {
            participantMetrics.SlotContention();
            participantMetrics.JoinAttempt("full");
            resolverContext.ReportError(
                TournamentErrors.TournamentFull(tournamentId, tournament.MaxParticipants));
            return null;
        }
        catch (DbUpdateException)
        {
            participantMetrics.JoinAttempt("already_joined");
            resolverContext.ReportError(
                TournamentErrors.UserAlreadyParticipant(userId, tournamentId));
            return null;
        }
    }

    [UseFirstOrDefault]
    [Authorize]
    public static async Task<IQueryable<Tournament>?> CreateTournament(
        CreateTournamentInput input,
        QueryContext<Tournament> query,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        var userId = userClaims.GetUserId();

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNameNotEmpty(input.Name)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsAtLeastTwo(input.MaxParticipants)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateStartDateHasMinimumLeadTime(input.StartDate, DateTime.UtcNow)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateStatusIsNotCompleted(input.Status)))
            return null;

        var format = input.Format ?? TournamentFormat.SingleElimination;
        var prizes = input.Prizes ?? [];

        if (resolverContext.TryReportError(TournamentValidations.ValidateEntryFeeNotNegative(input.EntryFeeAmount)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidatePrizeAmountsArePositive(prizes)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePlacements(prizes, input.MaxParticipants)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePlacementsSupportedByFormat(prizes, format)))
            return null;

        var prizePoolStatus = prizes.Count > 0 ? PrizePoolStatus.Unfunded : PrizePoolStatus.NotRequired;

        if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePoolAllowsOpening(0, prizePoolStatus, input.Status)))
            return null;

        if (input.Status == TournamentStatus.Open && input.EntryFeeAmount > 0)
        {
            var ownerPayoutStatus = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.PayoutAccountStatus)
                .FirstAsync(token);

            if (resolverContext.TryReportError(PaymentValidations.ValidatePayoutAccountEnabledForPaidTournament(userId, input.EntryFeeAmount, ownerPayoutStatus)))
                return null;
        }

        var tournament = new Tournament
        {
            Name = input.Name,
            StartDate = input.StartDate,
            Status = input.Status,
            OwnerId = userId,
            MaxParticipants = input.MaxParticipants,
            Format = format,
            EntryFeeAmount = input.EntryFeeAmount,
            PrizePoolStatus = prizePoolStatus,
            Prizes = prizes.Select(p => new TournamentPrize { Placement = p.Placement, Amount = p.Amount }).ToList()
        };

        context.Tournaments.Add(tournament);
        await context.SaveChangesAsync(token);

        using var _ = resolverContext.PushEntityContext("Tournament", tournament.Id);

        tournamentMetrics.IncrementTournamentsCreated();
        if (tournament.Status == TournamentStatus.Open)
            tournamentMetrics.TournamentOpened();

        return context.Tournaments.AsNoTracking().Where(t => t.Id == tournament.Id).With(query);
    }

    [UseFirstOrDefault]
    [Authorize]
    public static async Task<IQueryable<Tournament>?> UpdateTournament(
        UpdateTournamentInput input,
        QueryContext<Tournament> query,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        using var _ = resolverContext.PushEntityContext("Tournament", input.TournamentId);

        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Prizes)
            .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, input.TournamentId)))
            return null;

        if (tournament is null)
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament.OwnerId, userId, input.TournamentId)))
            return null;

        var termsChanged = input.EntryFeeAmount != null || input.Prizes != null;

        if (input.Name != null)
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentNameNotEmpty(input.Name)))
                return null;

            tournament.Name = input.Name;
        }

        if (input.StartDate != null)
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidateStartDateHasMinimumLeadTime(input.StartDate.Value, DateTime.UtcNow)))
                return null;

            tournament.StartDate = input.StartDate.Value;
        }

        if (input.MaxParticipants != null)
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsAtLeastTwo(input.MaxParticipants.Value)))
                return null;

            if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotClosed(tournament, DateTime.UtcNow)))
                return null;

            var currentParticipantCount = await context.TournamentParticipants
                .CountAsync(tp => tp.TournamentId == tournament.Id, token);

            if (resolverContext.TryReportError(TournamentValidations.ValidateMaxParticipantsNotBelowParticipantCount(tournament.Id, currentParticipantCount, input.MaxParticipants.Value)))
                return null;

            tournament.MaxParticipants = input.MaxParticipants.Value;
        }

        if (termsChanged)
        {
            var entryCount = await context.TournamentParticipants
                .CountAsync(tp => tp.TournamentId == tournament.Id, token);

            if (resolverContext.TryReportError(TournamentValidations.ValidatePaidTermsCanChange(tournament.Id, tournament.PrizePoolStatus, entryCount)))
                return null;

            if (input.EntryFeeAmount != null)
            {
                if (resolverContext.TryReportError(TournamentValidations.ValidateEntryFeeNotNegative(input.EntryFeeAmount.Value)))
                    return null;

                tournament.EntryFeeAmount = input.EntryFeeAmount.Value;
            }

            if (input.Prizes != null)
            {
                if (resolverContext.TryReportError(TournamentValidations.ValidatePrizeAmountsArePositive(input.Prizes)))
                    return null;

                ReplacePrizes(context, tournament, input.Prizes);
                tournament.PrizePoolStatus = input.Prizes.Count > 0 ? PrizePoolStatus.Unfunded : PrizePoolStatus.NotRequired;
            }
        }

        if (termsChanged || input.MaxParticipants != null)
        {
            IReadOnlyList<PrizeInput> currentPrizes = input.Prizes
                ?? tournament.Prizes.Select(p => new PrizeInput(p.Placement, p.Amount)).ToList();

            if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePlacements(currentPrizes, tournament.MaxParticipants)))
                return null;

            if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePlacementsSupportedByFormat(currentPrizes, tournament.Format)))
                return null;
        }

        if (input.Status != null)
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentIsNotCompleted(tournament)))
                return null;

            if (resolverContext.TryReportError(TournamentValidations.ValidateStatusIsNotCompleted(input.Status.Value)))
                return null;

            var bracketExists = await context.Brackets.AnyAsync(b => b.TournamentId == tournament.Id, token);

            if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentCanBeReopened(tournament.Id, bracketExists, input.Status.Value, tournament.StartDate, DateTime.UtcNow)))
                return null;
        }

        if (input.Status != null || (termsChanged && tournament.Status == TournamentStatus.Open))
        {
            if (resolverContext.TryReportError(TournamentValidations.ValidatePrizePoolAllowsOpening(tournament.Id, tournament.PrizePoolStatus, input.Status ?? tournament.Status)))
                return null;

            if ((input.Status ?? tournament.Status) == TournamentStatus.Open)
            {
                var ownerPayoutStatus = await context.Users
                    .Where(u => u.Id == tournament.OwnerId)
                    .Select(u => u.PayoutAccountStatus)
                    .FirstAsync(token);

                if (resolverContext.TryReportError(PaymentValidations.ValidatePayoutAccountEnabledForPaidTournament(tournament.OwnerId, tournament.EntryFeeAmount, ownerPayoutStatus)))
                    return null;
            }
        }

        if (input.Status != null)
        {
            var previousStatus = tournament.Status;
            tournament.Status = input.Status.Value;

            if (previousStatus != TournamentStatus.Open && tournament.Status == TournamentStatus.Open)
                tournamentMetrics.TournamentOpened();
            else if (previousStatus == TournamentStatus.Open && tournament.Status != TournamentStatus.Open)
            {
                tournamentMetrics.TournamentClosed();
                tournamentMetrics.IncrementTournamentsClosed("manual");
            }
        }

        await context.SaveChangesAsync(token);

        return context.Tournaments.AsNoTracking().Where(t => t.Id == tournament.Id).With(query);
    }

    private static void ReplacePrizes(ApplicationDbContext context, Tournament tournament, IReadOnlyList<PrizeInput> prizes)
    {
        foreach (var existing in tournament.Prizes.Where(e => prizes.All(p => p.Placement != e.Placement)).ToList())
            context.Remove(existing);

        foreach (var prize in prizes)
        {
            var existing = tournament.Prizes.FirstOrDefault(e => e.Placement == prize.Placement);
            if (existing is null)
                tournament.Prizes.Add(new TournamentPrize { Placement = prize.Placement, Amount = prize.Amount });
            else
                existing.Amount = prize.Amount;
        }
    }

    [Authorize]
    public static async Task<bool?> DeleteTournament(
        int tournamentId,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        TournamentMetrics tournamentMetrics,
        CancellationToken token)
    {
        using var _ = resolverContext.PushEntityContext("Tournament", tournamentId);

        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Bracket)
                .ThenInclude(b => b.Matches)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == tournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, tournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateIsOwner(tournament!.OwnerId, userId, tournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentCanBeDeleted(tournament)))
            return null;

        var wasOpen = tournament!.Status == TournamentStatus.Open;

        context.Tournaments.Remove(tournament);

        await context.SaveChangesAsync(token);

        if (wasOpen)
            tournamentMetrics.TournamentClosed();

        tournamentMetrics.IncrementTournamentsDeleted();

        return true;
    }

    [Authorize]
    public static async Task<bool?> WithdrawParticipant(
        WithdrawParticipantInput input,
        ClaimsPrincipal userClaims,
        ApplicationDbContext context,
        IResolverContext resolverContext,
        IEnumerable<IBracketCompletionStrategy> completionStrategies,
        CancellationToken token)
    {
        using var _ = resolverContext.PushEntityContext("Tournament", input.TournamentId);

        var userId = userClaims.GetUserId();

        var tournament = await context.Tournaments
            .Include(t => t.Bracket)
                .ThenInclude(b => b!.Matches)
            .Include(t => t.Participants)
            .FirstOrDefaultAsync(t => t.Id == input.TournamentId, token);

        if (resolverContext.TryReportError(TournamentValidations.ValidateTournamentExists(tournament, input.TournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateCanWithdrawParticipant(tournament!.OwnerId, userId, input.ParticipantId, input.TournamentId)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateFormatSupportsWithdrawal(tournament)))
            return null;

        if (resolverContext.TryReportError(TournamentValidations.ValidateBracketGeneratedForWithdrawal(tournament.Bracket != null, input.TournamentId)))
            return null;

        var participant = tournament.Participants.FirstOrDefault(p => p.ParticipantId == input.ParticipantId);

        if (resolverContext.TryReportError(participant is null ? TournamentErrors.ParticipantNotFound(input.TournamentId, input.ParticipantId) : null))
            return null;

        var scheduledMatches = tournament.Bracket!.Matches
            .Where(m => m.Status == MatchStatus.Scheduled && (m.Player1Id == input.ParticipantId || m.Player2Id == input.ParticipantId));

        foreach (var match in scheduledMatches)
        {
            match.WinnerId = match.Player1Id == input.ParticipantId ? match.Player2Id!.Value : match.Player1Id;
            match.Status = MatchStatus.Played;
        }

        context.TournamentParticipants.Remove(participant!);

        var completionStrategy = completionStrategies.Single(s => s.Format == tournament.Format);
        await completionStrategy.SyncCompletionAsync(context, tournament, tournament.Bracket!.Id, frontierRound: 0, token);

        await context.SaveChangesAsync(token);

        return true;
    }
}
