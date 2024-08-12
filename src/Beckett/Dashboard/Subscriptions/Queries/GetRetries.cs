using Beckett.Database;
using Beckett.Subscriptions.Retries;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetRetries : IPostgresDatabaseQuery<GetRetriesResult>
{
    public async Task<GetRetriesResult> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            SELECT c.group_name, c.name, c.stream_name, c.stream_position, c.retry_id, r.attempts, r.retry_at, r.status
            FROM {schema}.checkpoints c
            INNER JOIN {schema}.retries r ON c.retry_id = r.id
            WHERE c.status = 'retry'
            AND c.retry_id IS NOT NULL
            ORDER BY c.group_name, c.name, c.stream_name, c.stream_position;
        ";

        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetRetriesResult.Retry>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(4))
            {
                continue;
            }

            results.Add(
                new GetRetriesResult.Retry(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<long>(3),
                    reader.GetFieldValue<Guid>(4),
                    reader.GetFieldValue<int>(5),
                    reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                    reader.GetFieldValue<RetryStatus>(7)
                )
            );
        }

        return new GetRetriesResult(results);
    }
}
