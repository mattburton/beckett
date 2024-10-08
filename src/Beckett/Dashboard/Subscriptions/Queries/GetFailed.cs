using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetFailed(PostgresOptions options) : IPostgresDatabaseQuery<GetFailedResult>
{
    public async Task<GetFailedResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id, group_name, name, stream_name, stream_position
            FROM {options.Schema}.checkpoints
            WHERE status = 'failed'
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
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<long>(4)
                )
            );
        }

        return new GetFailedResult(results);
    }
}
