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
                   s.id,
                   g.name,
                   s.name,
                   st.name,
                   c.stream_version,
                   c.stream_position,
                   c.status,
                   c.process_at,
                   c.reserved_until,
                   c.retries
            FROM {options.Schema}.checkpoints c
            INNER JOIN {options.Schema}.subscriptions s ON c.subscription_id = s.id
            INNER JOIN {options.Schema}.streams st ON c.stream_id = st.id
            INNER JOIN {options.Schema}.groups g ON s.group_id = g.id
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
            SubscriptionId = reader.GetFieldValue<int>(1),
            GroupName = reader.GetFieldValue<string>(2),
            SubscriptionName = reader.GetFieldValue<string>(3),
            StreamName = reader.GetFieldValue<string>(4),
            StreamVersion = reader.GetFieldValue<long>(5),
            StreamPosition = reader.GetFieldValue<long>(6),
            Status = reader.GetFieldValue<CheckpointStatus>(7),
            ProcessAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            ReservedUntil = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
            Retries = reader.IsDBNull(10) ? [] : reader.GetFieldValue<RetryType[]>(10)
        };
    }
}
