using TournamentAPI.Data.Models;

namespace TournamentAPI.Users;

public class ApplicationUserService(IApplicationUserBatchingContext batchingContext)
{
    public async Task<ApplicationUser?> GetApplicationUserByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await batchingContext.ApplicationUserById.LoadAsync(id, cancellationToken);
    }
}
