using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

internal static class RecordCheckpointQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string schema,
        string subscriptionName,
        string streamName,
        long checkpoint,
        bool blocked,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.record_checkpoint($1, $2, $3, $4);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = subscriptionName;
        command.Parameters[1].Value = streamName;
        command.Parameters[2].Value = checkpoint;
        command.Parameters[3].Value = blocked;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
