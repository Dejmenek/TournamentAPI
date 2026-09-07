namespace TournamentAPI.Data.Models;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
