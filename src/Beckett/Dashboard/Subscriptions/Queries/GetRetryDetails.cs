using Beckett.Database;
using Beckett.Subscriptions.Retries;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetRetryDetails(Guid id) : IPostgresDatabaseQuery<GetRetryDetailsResult?>
{
    public async Task<GetRetryDetailsResult?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        var batch = new NpgsqlBatch(command.Connection);

        var retryRow = new NpgsqlBatchCommand($@"
            SELECT id,
                   group_name,
                   name,
                   stream_name,
                   stream_position,
                   CASE WHEN status = 'reserved' THEN previous_status ELSE status END AS status,
                   error,
                   started_at,
                   retry_at,
                   attempts
            FROM {schema}.retries
            WHERE id = $1;
        ");

        retryRow.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid, Value = id });

        var eventRows = new NpgsqlBatchCommand($@"
            SELECT status,
                   timestamp,
                   error
            FROM {schema}.retry_events
            WHERE retry_id = $1
            AND attempt > 0
            ORDER BY timestamp DESC;
        ");

        eventRows.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid, Value = id });

        batch.BatchCommands.Add(retryRow);
        batch.BatchCommands.Add(eventRows);

        await using var reader = await batch.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        var result = new GetRetryDetailsResult
        {
            Id = reader.GetFieldValue<Guid>(0),
            GroupName = reader.GetFieldValue<string>(1),
            SubscriptionName = reader.GetFieldValue<string>(2),
            StreamName = reader.GetFieldValue<string>(3),
            StreamPosition = reader.GetFieldValue<long>(4),
            Status = reader.GetFieldValue<RetryStatus>(5),
            Exception = reader.IsDBNull(6) ? null : ExceptionData.FromJson(reader.GetFieldValue<string>(6)),
            StartedAt = reader.GetFieldValue<DateTimeOffset>(7),
            RetryAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            TotalAttempts = reader.GetFieldValue<int>(9),
            Attempts = []
        };

        await reader.NextResultAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Attempts.Add(new GetRetryDetailsResult.Attempt(
                reader.GetFieldValue<RetryStatus>(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.IsDBNull(2) ? null : ExceptionData.FromJson(reader.GetFieldValue<string>(2))
            ));
        }

        return result;
    }
}
