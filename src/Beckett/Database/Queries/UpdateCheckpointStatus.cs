using Beckett.Database.Types;
using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class UpdateCheckpointStatus(
    string application,
    string name,
    string streamName,
    long streamPosition,
    CheckpointStatus status
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.update_checkpoint_status($1, $2, $3, $4, $5);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStatus(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;
        command.Parameters[3].Value = streamPosition;
        command.Parameters[4].Value = status;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
