using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class AdvisoryUnlock(long advisoryLockId) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select pg_advisory_unlock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
