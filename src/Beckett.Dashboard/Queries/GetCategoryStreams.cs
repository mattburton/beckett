using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Queries;

public class GetCategoryStreams(string category) : IPostgresDatabaseQuery<IReadOnlyList<GetCategoryStreams.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select stream_name, max(timestamp) as last_updated
            from {schema}.messages
            where {schema}.stream_category(stream_name) = $1
            group by stream_name
            order by max(timestamp) desc;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = category;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<DateTimeOffset>(1)
                )
            );
        }

        return results;
    }

    public record Result(string StreamName, DateTimeOffset LastUpdated);
}
