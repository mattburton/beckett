using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetFailed(int offset, int limit, PostgresOptions options) : IPostgresDatabaseQuery<GetFailedResult>
{
    public async Task<GetFailedResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id, group_name, name, stream_name, stream_position, count(*) over() as total_results
            FROM {options.Schema}.checkpoints
            WHERE status = 'failed'
            ORDER BY group_name, name, stream_name, stream_position
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

        var results = new List<GetFailedResult.Failure>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(5);

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

        return new GetFailedResult(results, totalResults.GetValueOrDefault(0));
    }
}
