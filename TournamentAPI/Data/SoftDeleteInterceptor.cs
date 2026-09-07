using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;
using TournamentAPI.Data.Models;

namespace TournamentAPI.Data;

public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return ValueTask.FromResult(result);

        var visited = new HashSet<ISoftDeletable>(ReferenceEqualityComparer.Instance);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            visited.Add(entry.Entity);

            CascadeSoftDelete(eventData.Context, entry.Entity, visited);
        }

        return ValueTask.FromResult(result);
    }

    private static void CascadeSoftDelete(DbContext context, ISoftDeletable parentEntity, HashSet<ISoftDeletable> visited)
    {
        var parentEntry = context.Entry(parentEntity);

        foreach (var navigation in parentEntry.Navigations)
        {
            if (navigation.CurrentValue is null) continue;

            if (navigation.CurrentValue is IEnumerable<ISoftDeletable> children)
            {
                foreach (var child in children.ToList())
                {
                    if (child.IsDeleted || !visited.Add(child)) continue;

                    child.IsDeleted = true;
                    context.Entry(child).State = EntityState.Modified;
                    CascadeSoftDelete(context, child, visited);
                }
            }
            else if (navigation.CurrentValue is ISoftDeletable child && !child.IsDeleted && visited.Add(child))
            {
                child.IsDeleted = true;
                context.Entry(child).State = EntityState.Modified;
                CascadeSoftDelete(context, child, visited);
            }
        }
    }
}
