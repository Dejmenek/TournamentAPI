using HotChocolate.Resolvers;

namespace TournamentAPI.Extensions;

public static class ResolverContextExtensions
{
    public static bool TryReportError(this IResolverContext resolverContext, IError? error)
    {
        if (error is null) return false;
        resolverContext.ReportError(error);
        return true;
    }
}
