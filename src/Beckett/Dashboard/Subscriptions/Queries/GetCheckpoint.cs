using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetCheckpoint(long id, PostgresOptions options) : IPostgresDatabaseQuery<GetCheckpointResult?>
{
    public async Task<GetCheckpointResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id,
                   group_name,
                   name,
                   stream_name,
                   stream_version,
                   stream_position,
                   status,
                   process_at,
                   retries
            FROM {options.Schema}.checkpoints
            WHERE id = $1;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new GetCheckpointResult{
            Id = reader.GetFieldValue<long>(0),
            GroupName = reader.GetFieldValue<string>(1),
            Name = reader.GetFieldValue<string>(2),
            StreamName = reader.GetFieldValue<string>(3),
            StreamVersion = reader.GetFieldValue<long>(4),
            StreamPosition = reader.GetFieldValue<long>(5),
            Status = reader.GetFieldValue<CheckpointStatus>(6),
            ProcessAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            Retries = reader.IsDBNull(8) ? [] : reader.GetFieldValue<RetryType[]>(8)
        };
    }
}
