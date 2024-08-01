using Beckett.Database.Types;
using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class UpdateCheckpointStatus(
    long checkpointId,
    long streamPosition,
    CheckpointStatus status,
    string? lastError = null
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.update_checkpoint_status($1, $2, $3, $4);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus(schema) });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, IsNullable = true });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = checkpointId;
        command.Parameters[1].Value = streamPosition;
        command.Parameters[2].Value = status;
        command.Parameters[3].Value = lastError == null ? DBNull.Value : lastError;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
