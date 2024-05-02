using Npgsql;

namespace Beckett.Storage.Postgres;

public static class NpgsqlConnectionExtensions
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

    public static async Task AdvisoryUnlock(
        this NpgsqlConnection connection,
        long advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select pg_advisory_unlock({advisoryLockId});";

        await command.ExecuteScalarAsync(cancellationToken);
    }
}
