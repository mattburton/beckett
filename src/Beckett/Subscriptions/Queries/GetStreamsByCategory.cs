using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetStreamsByCategory(
    string category
) : IPostgresDatabaseQuery<IReadOnlyList<GetStreamsByCategory.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT si.stream_name, si.latest_position
            FROM beckett.stream_index si
            INNER JOIN beckett.stream_categories sc ON si.stream_category_id = sc.id
            WHERE sc.name = $1
            ORDER BY si.stream_name;
        """;

        command.CommandText = Query.Build(nameof(GetStreamsByCategory), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = category;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<long>(1)
                )
            );
        }

        return results;
    }

    public record Result(
        string StreamName,
        long LatestPosition
    );
}