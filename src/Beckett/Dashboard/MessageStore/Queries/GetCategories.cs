using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetCategories(string? query) : IPostgresDatabaseQuery<GetCategoriesResult>
{
    public async Task<GetCategoriesResult> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select {schema}.stream_category(stream_name) as name, max(timestamp) as last_updated
            from {schema}.messages
            where ($1 is null or {schema}.stream_category(stream_name) ilike '%' || $1 || '%')
            and deleted = false
            group by {schema}.stream_category(stream_name)
            order by {schema}.stream_category(stream_name);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetCategoriesResult.Category>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new GetCategoriesResult.Category(reader.GetFieldValue<string>(0), reader.GetFieldValue<DateTimeOffset>(1)));
        }

        return new GetCategoriesResult(results);
    }
}
