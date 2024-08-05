using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateCheckpointStatus(
    string groupName,
    string name,
    string streamName,
    long streamPosition,
    CheckpointStatus status,
    int? maxRetries = null,
    string? error = null
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.update_checkpoint_status($1, $2, $3, $4, $5, $6, $7);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus(schema) });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, IsNullable = true });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;
        command.Parameters[3].Value = streamPosition;
        command.Parameters[4].Value = status;
        command.Parameters[5].Value = maxRetries == null ? DBNull.Value : maxRetries;
        command.Parameters[6].Value = error == null ? DBNull.Value : error;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
