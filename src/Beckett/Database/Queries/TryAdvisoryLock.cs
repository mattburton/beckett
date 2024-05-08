using Npgsql;

namespace Beckett.Database.Queries;

public class TryAdvisoryLock(long advisoryLockId) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select pg_try_advisory_lock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
