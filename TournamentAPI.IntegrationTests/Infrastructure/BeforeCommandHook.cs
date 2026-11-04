using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public sealed class BeforeCommandHook : DbCommandInterceptor
{
    private readonly object _lock = new();
    private Func<DbCommand, bool>? _match;
    private Action? _action;

    public void RunOnce(Func<DbCommand, bool> match, Action action)
    {
        lock (_lock)
        {
            _match = match;
            _action = action;
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _match = null;
            _action = null;
        }
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Run(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Run(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Run(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Run(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Run(DbCommand command)
    {
        Action? action;
        lock (_lock)
        {
            if (_match is null || !_match(command))
                return;

            action = _action;
            _match = null;
            _action = null;
        }

        action?.Invoke();
    }
}
