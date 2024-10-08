using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetRetries (PostgresOptions options) : IPostgresDatabaseQuery<GetRetriesResult>
{
    public async Task<GetRetriesResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.id, c.group_name, c.name, c.stream_name, c.stream_position
            FROM {options.Schema}.checkpoints c
            WHERE c.status = 'retry'
            ORDER BY c.group_name, c.name, c.stream_name, c.stream_position;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

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
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<long>(4)
                )
            );
        }

        return new GetRetriesResult(results);
    }
}
