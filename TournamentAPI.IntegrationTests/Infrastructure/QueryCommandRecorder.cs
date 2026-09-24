using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TournamentAPI.IntegrationTests.Infrastructure;

public class QueryCommandRecorder : DbCommandInterceptor
{
    private readonly List<string> _commands = new();
    private readonly object _lock = new();

    public IReadOnlyList<string> Commands
    {
        get
        {
            lock (_lock)
            {
                return _commands.ToList();
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _commands.Clear();
        }
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command)
    {
        lock (_lock)
        {
            _commands.Add(command.CommandText);
        }
    }
}
