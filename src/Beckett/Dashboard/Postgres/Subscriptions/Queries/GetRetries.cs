using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.Subscriptions.Queries;

public class GetRetries(
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetRetriesResult>
{
    public async Task<GetRetriesResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT c.id,
                   g.name,
                   s.name,
                   st.name,
                   c.stream_position,
                   c.updated_at,
                   count(*) over() as total_results
            FROM {options.Schema}.checkpoints c
            INNER JOIN {options.Schema}.subscriptions s ON c.subscription_id = s.id
            INNER JOIN {options.Schema}.streams st ON c.stream_id = st.id
            INNER JOIN {options.Schema}.groups g ON s.group_id = g.id
            WHERE c.status = 'retry'
            AND ($1 is null or (g.name ILIKE '%' || $1 || '%' OR s.name ILIKE '%' || $1 || '%' OR st.name ILIKE '%' || $1 || '%'))
            ORDER BY c.updated_at desc, g.name, s.name, st.name, c.stream_position
            OFFSET $2
            LIMIT $3;
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

        var results = new List<GetRetriesResult.Retry>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(6);

            results.Add(
                new GetRetriesResult.Retry(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<long>(4),
                    reader.GetFieldValue<DateTimeOffset>(5)
                )
            );
        }

        return new GetRetriesResult(results, totalResults.GetValueOrDefault(0));
    }
}
