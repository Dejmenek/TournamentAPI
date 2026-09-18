using HotChocolate.Resolvers;
using Serilog.Context;

namespace TournamentAPI.Extensions;

public static class ResolverContextExtensions
{
    public static bool TryReportError(this IResolverContext resolverContext, IError? error)
    {
        if (error is null) return false;
        resolverContext.ReportError(error);
        return true;
    }

    /// <summary>
    /// Pushes the entity into the Serilog LogContext (for logs emitted during this resolver)
    /// and into the request's GraphQL context data (so ExecutionEventListener's request-level
    /// completion log, which runs after the resolver's LogContext scope has already unwound,
    /// can still report it).
    /// </summary>
    public static IDisposable PushEntityContext(this IResolverContext resolverContext, string entityType, object entityId)
    {
        resolverContext.ContextData["EntityType"] = entityType;
        resolverContext.ContextData["EntityId"] = entityId;

        return LogContext.PushProperty(entityType + "Id", entityId);
    }
}
