using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public static class RecordCheckpointQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string subscriptionName,
        string streamName,
        long checkpoint,
        bool blocked,
        CancellationToken cancellationToken
    )
    {
        const string sql = "select record_checkpoint($1, $2, $3, $4);";

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

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
