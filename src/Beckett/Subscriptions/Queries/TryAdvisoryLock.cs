using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class TryAdvisoryLock(long advisoryLockId) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select pg_try_advisory_lock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
