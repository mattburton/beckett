using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpointError(
    string groupName,
    string name,
    string streamName,
    long streamPosition,
    string error,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.record_checkpoint_error($1, $2, $3, $4, $5);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;
        command.Parameters[3].Value = streamPosition;
        command.Parameters[4].Value = error;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
