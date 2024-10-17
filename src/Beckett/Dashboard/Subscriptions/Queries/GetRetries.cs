using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetRetries(int offset, int limit, PostgresOptions options) : IPostgresDatabaseQuery<GetRetriesResult>
{
    public async Task<GetRetriesResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.id, c.group_name, c.name, c.stream_name, c.stream_position, count(*) over() as total_results
            FROM {options.Schema}.checkpoints c
            WHERE c.status = 'retry'
            ORDER BY c.group_name, c.name, c.stream_name, c.stream_position
            OFFSET $1
            LIMIT $2;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = offset;
        command.Parameters[1].Value = limit;


        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetRetriesResult.Retry>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(5);

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

        return new GetRetriesResult(results, totalResults.GetValueOrDefault(0));
    }
}
