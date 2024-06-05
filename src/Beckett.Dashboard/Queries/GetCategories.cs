using Npgsql;

namespace Beckett.Dashboard.Queries;

public class GetCategories : IPostgresDatabaseQuery<IReadOnlyList<GetCategories.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(
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

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Result(reader.GetFieldValue<string>(0), reader.GetFieldValue<DateTimeOffset>(1)));
        }

        return results;
    }

    public record Result(string Name, DateTimeOffset LastUpdated);
}
