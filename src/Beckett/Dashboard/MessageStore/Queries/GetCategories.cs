using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetCategories(
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetCategoriesResult>
{
    public async Task<GetCategoriesResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select {options.Schema}.stream_category(stream_name) as name,
                   max(timestamp) as last_updated,
                   count(*) over() as total_results
            from {options.Schema}.messages
            where ($1 is null or {options.Schema}.stream_category(stream_name) ilike '%' || $1 || '%')
            and deleted = false
            group by {options.Schema}.stream_category(stream_name)
            order by {options.Schema}.stream_category(stream_name)
            offset $2
            limit $3;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[1].Value = offset;
        command.Parameters[2].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetCategoriesResult.Category>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(2);

            results.Add(
                new GetCategoriesResult.Category(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1)
                )
            );
        }

        return new GetCategoriesResult(results, totalResults.GetValueOrDefault(0));
    }
}
