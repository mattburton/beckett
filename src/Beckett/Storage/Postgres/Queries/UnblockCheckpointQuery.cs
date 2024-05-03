using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class UnblockCheckpointQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string schema,
        string subscriptionName,
        string streamName,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.record_checkpoint($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = subscriptionName;
        command.Parameters[1].Value = streamName;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
