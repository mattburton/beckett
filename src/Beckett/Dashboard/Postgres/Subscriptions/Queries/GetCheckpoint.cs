using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.Subscriptions.Queries;

public class GetCheckpoint(long id, PostgresOptions options) : IPostgresDatabaseQuery<GetCheckpointResult?>
{
    public async Task<GetCheckpointResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.id,
                   c.group_name,
                   c.name,
                   c.stream_name,
                   c.stream_version,
                   c.stream_position,
                   c.status,
                   c.process_at,
                   c.reserved_until,
                   c.retries,
                   m.stream_name as actual_stream_name,
                   m.stream_position as actual_stream_position
            FROM {options.Schema}.checkpoints c
            LEFT JOIN {options.Schema}.messages m on c.stream_name = '$global' and c.stream_position = m.global_position
            WHERE c.id = $1;
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
            ReservedUntil = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            Retries = reader.IsDBNull(9) ? [] : reader.GetFieldValue<RetryType[]>(9),
            ActualStreamName = reader.IsDBNull(10) ? null : reader.GetFieldValue<string>(10),
            ActualStreamPosition = reader.IsDBNull(11) ? null : reader.GetFieldValue<long>(11)
        };
    }
}
