using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class UpdateCheckpointStreamPosition(
    string application,
    string name,
    string topic,
    string streamId,
    long streamPosition,
    bool blocked
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.update_checkpoint_stream_position($1, $2, $3, $4, $5, $6);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = topic;
        command.Parameters[3].Value = streamId;
        command.Parameters[4].Value = streamPosition;
        command.Parameters[5].Value = blocked;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
