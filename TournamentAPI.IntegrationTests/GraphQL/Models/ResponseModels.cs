using TournamentAPI.IntegrationTests.GraphQL.Helpers;

namespace TournamentAPI.IntegrationTests.GraphQL.Models;

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
    public ApplicationUserNode? Owner { get; set; }
    public BracketNode? Bracket { get; set; }
    public List<TournamentParticipantNode>? Participants { get; set; }
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
    public string Email { get; set; } = string.Empty;
}

public class BracketNode
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public List<MatchNode>? Matches { get; set; }
}

public class MatchNode
{
    public int Id { get; set; }
    public int Round { get; set; }
    public int BracketId { get; set; }
    public int Player1Id { get; set; }
    public int? Player2Id { get; set; }
    public int? WinnerId { get; set; }
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
    public List<GraphQLError>? Errors { get; set; }
}

public class UpdateTournamentResponse
{
    public UpdateTournamentResult? UpdateTournament { get; set; }
}

public class UpdateTournamentResult
{
    public TournamentNode? Tournament { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class DeleteTournamentResponse
{
    public DeleteTournamentResult? DeleteTournament { get; set; }
}

public class DeleteTournamentResult
{
    public bool? Boolean { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class AddParticipantResponse
{
    public AddParticipantResult? AddParticipant { get; set; }
}

public class AddParticipantResult
{
    public TournamentNode? Tournament { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class JoinTournamentResponse
{
    public JoinTournamentResult? JoinTournament { get; set; }
}

public class JoinTournamentResult
{
    public bool? Boolean { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class GenerateBracketResponse
{
    public GenerateBracketResult? GenerateBracket { get; set; }
}

public class GenerateBracketResult
{
    public BracketNode? Bracket { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class PlayMatchResponse
{
    public PlayMatchResult? Play { get; set; }
}

public class PlayMatchResult
{
    public bool? Boolean { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class UpdateRoundResponse
{
    public UpdateRoundResult? UpdateRound { get; set; }
}

public class UpdateRoundResult
{
    public BracketNode? Bracket { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class MatchesForRoundResponse
{
    public List<MatchNode>? MatchesForRound { get; set; }
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
    public bool Boolean { get; set; }
}

public class MeResponse
{
    public UserNode? Me { get; set; }
}

public class UserNode
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
