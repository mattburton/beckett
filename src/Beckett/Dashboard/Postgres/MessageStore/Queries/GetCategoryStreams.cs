using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.MessageStore.Queries;

public class GetCategoryStreams(
    string tenant,
    string category,
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetCategoryStreamsResult>
{
    public async Task<GetCategoryStreamsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select stream_name,
                   max(timestamp) as last_updated,
                   count(*) over() as total_results
            from {options.Schema}.messages_active
            where metadata ->> '$tenant' = $1
            and {options.Schema}.stream_category(stream_name) = $2
            and ($3 is null or stream_name ilike '%' || $3 || '%')
            group by stream_name
            order by max(timestamp) desc
            offset $4
            limit $5;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = tenant;
        command.Parameters[1].Value = category;
        command.Parameters[2].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[3].Value = offset;
        command.Parameters[4].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetCategoryStreamsResult.Stream>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(2);

            results.Add(
                new GetCategoryStreamsResult.Stream(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1)
                )
            );
        }

        return new GetCategoryStreamsResult(results, totalResults.GetValueOrDefault(0));
    }
}
