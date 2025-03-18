using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpointStreamRetries(
    long checkpointId,
    CheckpointStreamRetryType[] retries,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.record_checkpoint_stream_retries($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(
            new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointStreamRetryArray(options.Schema) }
        );

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpointId;
        command.Parameters[1].Value = retries;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
