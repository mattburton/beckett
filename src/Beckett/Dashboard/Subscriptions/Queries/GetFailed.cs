using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetFailed(PostgresOptions options) : IPostgresDatabaseQuery<GetFailedResult>
{
    public async Task<GetFailedResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT group_name, name, stream_name, stream_position, retry_id
            FROM {options.Schema}.checkpoints
            WHERE status = 'failed'
            AND retry_id IS NOT NULL
            ORDER BY group_name, name, stream_name, stream_position;
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetFailedResult.Failure>();

        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(4))
            {
                continue;
            }

            results.Add(
                new GetFailedResult.Failure(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<long>(3),
                    reader.GetFieldValue<Guid>(4)
                )
            );
        }

        return new GetFailedResult(results);
    }
}
