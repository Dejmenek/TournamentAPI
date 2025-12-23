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
    public TournamentNode? CreateTournament { get; set; }
}

public class UpdateTournamentResponse
{
    public TournamentNode? UpdateTournament { get; set; }
}

public class DeleteTournamentResponse
{
    public bool DeleteTournament { get; set; }
}

public class AddParticipantResponse
{
    public TournamentNode? AddParticipant { get; set; }
}

public class JoinTournamentResponse
{
    public bool JoinTournament { get; set; }
}

public class GenerateBracketResponse
{
    public BracketNode? GenerateBracket { get; set; }
}

public class PlayMatchResponse
{
    public bool Play { get; set; }
}

public class UpdateRoundResponse
{
    public BracketNode? UpdateRound { get; set; }
}

public class MatchesForRoundResponse
{
    public List<MatchNode>? MatchesForRound { get; set; }
}

public class LoginResponse
{
    public string? LoginUser { get; set; }
}

public class RegisterResponse
{
    public bool RegisterUser { get; set; }
}

public class MeResponse
{
    public UserNode? Me { get; set; }
}

public class UserNode
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
