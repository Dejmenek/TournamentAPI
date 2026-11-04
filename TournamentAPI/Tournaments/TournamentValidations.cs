using TournamentAPI.Data.Models;

namespace TournamentAPI.Tournaments;

public static class TournamentValidations
{
    public static readonly TimeSpan MinimumStartDateLeadTime = TimeSpan.FromMinutes(30);

    public static IError? ValidateTournamentExists(Tournament? tournament, int tournamentId)
        => tournament == null ? TournamentErrors.TournamentNotFound(tournamentId) : null;

    public static IError? ValidateIsOwner(int ownerId, int userId, int tournamentId)
        => ownerId != userId ? TournamentErrors.TournamentNotOwner(userId, tournamentId) : null;

    public static IError? ValidateTournamentIsNotClosed(Tournament tournament, DateTime now)
        => !tournament.IsActive(now) ? TournamentErrors.TournamentClosed(tournament.Id) : null;

    public static IError? ValidateUserNotAlreadyParticipant(Tournament tournament, int userId)
        => tournament.Participants.Any(tp => tp.ParticipantId == userId) ? TournamentErrors.UserAlreadyParticipant(userId, tournament.Id) : null;

    public static IError? ValidateTournamentNameNotEmpty(string? name)
        => string.IsNullOrWhiteSpace(name) ? TournamentErrors.TournamentNameEmpty() : null;

    public static IError? ValidateTournamentNotFull(Tournament tournament)
        => tournament.Participants.Count >= tournament.MaxParticipants ? TournamentErrors.TournamentFull(tournament.Id, tournament.MaxParticipants) : null;

    public static IError? ValidateMaxParticipantsAtLeastTwo(int maxParticipants)
        => maxParticipants < 2 ? TournamentErrors.InvalidMaxParticipants(maxParticipants) : null;

    public static IError? ValidateMaxParticipantsNotBelowParticipantCount(int tournamentId, int currentParticipantCount, int maxParticipants)
        => maxParticipants < currentParticipantCount ? TournamentErrors.MaxParticipantsBelowParticipantCount(tournamentId, maxParticipants, currentParticipantCount) : null;

    public static IError? ValidateStartDateHasMinimumLeadTime(DateTime startDate, DateTime now)
        => startDate < now.Add(MinimumStartDateLeadTime) ? TournamentErrors.StartDateTooSoon(startDate) : null;

    public static IError? ValidateTournamentCanBeDeleted(Tournament tournament)
        => (tournament.Status == TournamentStatus.Closed || tournament.Status == TournamentStatus.Completed) && tournament.Bracket != null
            ? TournamentErrors.CannotDeleteTournamentWithBracket(tournament.Id)
            : null;

    public static IError? ValidateStatusIsNotCompleted(TournamentStatus requestedStatus)
        => requestedStatus == TournamentStatus.Completed ? TournamentErrors.TournamentStatusCannotBeSetManually() : null;

    public static IError? ValidateTournamentIsNotCompleted(Tournament tournament)
        => tournament.Status == TournamentStatus.Completed ? TournamentErrors.CannotChangeCompletedTournamentStatus(tournament.Id) : null;

    public static IError? ValidateTournamentCanBeReopened(int tournamentId, bool bracketExists, TournamentStatus newStatus, DateTime startDate, DateTime now)
    {
        if (newStatus != TournamentStatus.Open)
            return null;

        if (bracketExists)
            return TournamentErrors.CannotReopenTournamentWithBracket(tournamentId);

        if (startDate <= now)
            return TournamentErrors.CannotReopenTournamentAfterStartDate(tournamentId);

        return null;
    }

    public static IError? ValidateCanWithdrawParticipant(int ownerId, int callerId, int participantId, int tournamentId)
        => callerId == ownerId || callerId == participantId ? null : TournamentErrors.NotAuthorizedForWithdrawal(callerId, tournamentId, participantId);

    public static IError? ValidateFormatSupportsWithdrawal(Tournament tournament)
        => tournament.Format != TournamentFormat.RoundRobin ? TournamentErrors.WithdrawalNotSupportedForFormat(tournament.Id) : null;

    public static IError? ValidateBracketGeneratedForWithdrawal(bool bracketExists, int tournamentId)
        => bracketExists ? null : TournamentErrors.WithdrawalRequiresBracket(tournamentId);

    public static IError? ValidateEntryFeeNotNegative(long entryFeeAmount)
        => entryFeeAmount < 0 ? TournamentErrors.InvalidEntryFee(entryFeeAmount) : null;

    public static IError? ValidatePrizeAmountsArePositive(IEnumerable<PrizeInput> prizes)
    {
        var invalid = prizes.FirstOrDefault(p => p.Amount <= 0);
        return invalid is null ? null : TournamentErrors.InvalidPrizeAmount(invalid.Placement, invalid.Amount);
    }

    public static IError? ValidatePrizePlacements(IReadOnlyCollection<PrizeInput> prizes, int maxParticipants)
    {
        if (prizes.Count == 0)
            return null;

        var placements = prizes.Select(p => p.Placement).Order().ToList();
        var isContiguousFromOne = placements.Select((placement, index) => placement == index + 1).All(ok => ok);

        return isContiguousFromOne && placements.Count <= maxParticipants
            ? null
            : TournamentErrors.InvalidPrizePlacements(maxParticipants);
    }

    public static IError? ValidatePrizePlacementsSupportedByFormat(IReadOnlyCollection<PrizeInput> prizes, TournamentFormat format)
        => format == TournamentFormat.SingleElimination && prizes.Any(p => p.Placement > 2)
            ? TournamentErrors.PrizePlacementNotSupportedForFormat(format)
            : null;

    public static IError? ValidatePaidTermsCanChange(int tournamentId, PrizePoolStatus prizePoolStatus, int entryCount)
        => (prizePoolStatus is PrizePoolStatus.NotRequired or PrizePoolStatus.Unfunded) && entryCount == 0
            ? null
            : TournamentErrors.PaidTermsLocked(tournamentId);

    public static IError? ValidatePrizePoolAllowsOpening(int tournamentId, PrizePoolStatus prizePoolStatus, TournamentStatus newStatus)
        => newStatus == TournamentStatus.Open && prizePoolStatus is not (PrizePoolStatus.NotRequired or PrizePoolStatus.Funded)
            ? TournamentErrors.PrizePoolNotFunded(tournamentId)
            : null;
}
