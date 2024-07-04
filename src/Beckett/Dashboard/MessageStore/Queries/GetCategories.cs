using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetCategories : IPostgresDatabaseQuery<GetCategoriesResult>
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
            group by {schema}.stream_category(stream_name)
            order by {schema}.stream_category(stream_name);
        ";

        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetCategoriesResult.Category>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new GetCategoriesResult.Category(reader.GetFieldValue<string>(0), reader.GetFieldValue<DateTimeOffset>(1)));
        }

        return new GetCategoriesResult(results);
    }
}
