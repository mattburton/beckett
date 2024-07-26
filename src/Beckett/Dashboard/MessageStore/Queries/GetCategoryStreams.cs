using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.MessageStore.Queries;

public class GetCategoryStreams(string category, string? query) : IPostgresDatabaseQuery<GetCategoryStreamsResult>
{
    public async Task<GetCategoryStreamsResult> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select stream_name, max(timestamp) as last_updated
            from {schema}.messages
            where {schema}.stream_category(stream_name) = $1
            and ($2 is null or stream_name ilike '%' || $2 || '%')
            group by stream_name
            order by max(timestamp) desc;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true});

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = category;
        command.Parameters[1].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetCategoryStreamsResult.Stream>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new GetCategoryStreamsResult.Stream(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1)
                )
            );
        }

        return new GetCategoryStreamsResult(results);
    }

    public record Result(string StreamName, DateTimeOffset LastUpdated);
}
