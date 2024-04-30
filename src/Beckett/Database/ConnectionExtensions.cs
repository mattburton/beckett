using Npgsql;

namespace Beckett.Database;

public static class ConnectionExtensions
{
    public static async Task<bool> TryAdvisoryLock(
        this NpgsqlConnection connection,
        long advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select pg_try_advisory_lock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }

    public static async Task<bool> AdvisoryUnlock(
        this NpgsqlConnection connection,
        long advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select pg_advisory_unlock({advisoryLockId});";

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
