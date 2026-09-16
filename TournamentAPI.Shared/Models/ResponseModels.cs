namespace TournamentAPI.Shared.Models;

public class TournamentsResponse
{
    public TournamentsConnection? Tournaments { get; set; }
}

public class TournamentsConnection
{
    public int? TotalCount { get; set; }
    public PageInfo? PageInfo { get; set; }
    public List<TournamentEdge>? Edges { get; set; }

    public List<TournamentNode>? Nodes => Edges?.Select(e => e.Node).ToList();
}

public class TournamentEdge
{
    public string? Cursor { get; set; }
    public TournamentNode Node { get; set; } = null!;
}

public class TournamentByIdResponse
{
    public TournamentNode? TournamentById { get; set; }
}

public class TournamentNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public int OwnerId { get; set; }
    public int MaxParticipants { get; set; }
    public bool IsActive { get; set; }
    public ApplicationUserNode? Owner { get; set; }
    public BracketNode? Bracket { get; set; }
    public ParticipantsConnection? Participants { get; set; }
}

public class ParticipantsConnection
{
    public int? TotalCount { get; set; }
    public PageInfo? PageInfo { get; set; }
    public List<ParticipantEdge>? Edges { get; set; }

    public List<TournamentParticipantNode>? Nodes => Edges?.Select(e => e.Node).ToList();
}

public class ParticipantEdge
{
    public string? Cursor { get; set; }
    public TournamentParticipantNode Node { get; set; } = null!;
}

public class TournamentParticipantNode
{
    public int TournamentId { get; set; }
    public int ParticipantId { get; set; }
    public ApplicationUserNode? Participant { get; set; }
    public TournamentNode? Tournament { get; set; }
}

public class ApplicationUserNode
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsEmailPublic { get; set; }
    public TournamentsConnection? WonTournaments { get; set; }
    public TournamentsConnection? PlayedTournaments { get; set; }
}

public class BracketNode
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public MatchesByBracketConnection? MatchesByBracket { get; set; }
}

public class MatchesByBracketConnection
{
    public int? TotalCount { get; set; }
    public PageInfo? PageInfo { get; set; }
    public List<MatchEdge>? Edges { get; set; }

    public List<MatchNode>? Nodes => Edges?.Select(e => e.Node).ToList();
}

public class MatchEdge
{
    public string? Cursor { get; set; }
    public MatchNode Node { get; set; } = null!;
}

public class MatchNode
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int BracketId { get; set; }
    public int Player1Id { get; set; }
    public int? Player2Id { get; set; }
    public int? WinnerId { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public string? Status { get; set; }
    public string? Version { get; set; }
    public ApplicationUserNode? Player1 { get; set; }
    public ApplicationUserNode? Player2 { get; set; }
    public ApplicationUserNode? Winner { get; set; }
}

public class ParticipantNode
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class PageInfo
{
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? StartCursor { get; set; }
    public string? EndCursor { get; set; }
}

public class CreateTournamentResponse
{
    public CreateTournamentResult? CreateTournament { get; set; }
}

public class CreateTournamentResult
{
    public TournamentNode? Tournament { get; set; }
}

public class UpdateTournamentResponse
{
    public UpdateTournamentResult? UpdateTournament { get; set; }
}

public class UpdateTournamentResult
{
    public TournamentNode? Tournament { get; set; }
}

public class DeleteTournamentResponse
{
    public DeleteTournamentResult? DeleteTournament { get; set; }
}

public class DeleteTournamentResult
{
    public bool? Boolean { get; set; }
}

public class AddParticipantResponse
{
    public AddParticipantResult? AddParticipant { get; set; }
}

public class AddParticipantResult
{
    public TournamentNode? Tournament { get; set; }
}

public class JoinTournamentResponse
{
    public JoinTournamentResult? JoinTournament { get; set; }
}

public class JoinTournamentResult
{
    public bool? Boolean { get; set; }
}

public class GenerateBracketResponse
{
    public GenerateBracketResult? GenerateBracket { get; set; }
}

public class GenerateBracketResult
{
    public BracketNode? Bracket { get; set; }
}

public class PlayMatchResponse
{
    public PlayMatchResult? Play { get; set; }
}

public class PlayMatchResult
{
    public bool? Boolean { get; set; }
}

public class CorrectMatchResultResponse
{
    public CorrectMatchResultResult? CorrectMatchResult { get; set; }
}

public class CorrectMatchResultResult
{
    public bool? Boolean { get; set; }
}

public class UpdateRoundResponse
{
    public UpdateRoundResult? UpdateRound { get; set; }
}

public class UpdateRoundResult
{
    public BracketNode? Bracket { get; set; }
}

public class LoginResponse
{
    public LoginUserResult? LoginUser { get; set; }
}

public class LoginUserResult
{
    public string? String { get; set; }
}

public class RegisterResponse
{
    public RegisterUserResult? RegisterUser { get; set; }
}

public class RegisterUserResult
{
    public bool? Boolean { get; set; }
}

public class RefreshTokenResponse
{
    public RefreshTokenResult? RefreshToken { get; set; }
}

public class RefreshTokenResult
{
    public string? String { get; set; }
}

public class LogoutResponse
{
    public LogoutUserResult? LogoutUser { get; set; }
}

public class LogoutUserResult
{
    public bool? Boolean { get; set; }
}

public class MeResponse
{
    public UserNode? Me { get; set; }
}

public class UserNode
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsEmailPublic { get; set; }
    public TournamentsConnection? WonTournaments { get; set; }
    public TournamentsConnection? PlayedTournaments { get; set; }
}

public class UpdateEmailVisibilityResponse
{
    public UpdateEmailVisibilityResult? UpdateEmailVisibility { get; set; }
}

public class UpdateEmailVisibilityResult
{
    public UserNode? ApplicationUser { get; set; }
}
