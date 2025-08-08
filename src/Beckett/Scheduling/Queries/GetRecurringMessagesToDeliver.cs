using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class GetRecurringMessagesToDeliver(
    int batchSize
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresRecurringMessage>>
{
    public async Task<IReadOnlyList<PostgresRecurringMessage>> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        //language=sql
        const string sql = """
            SELECT
                name,
                cron_expression,
                time_zone_id,
                stream_name,
                type,
                data,
                metadata,
                next_occurrence,
                timestamp
            FROM beckett.recurring_messages
            WHERE next_occurrence <= CURRENT_TIMESTAMP
            FOR UPDATE
            SKIP LOCKED
            LIMIT $1;
        """;

        command.CommandText = Query.Build(nameof(GetRecurringMessagesToDeliver), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresRecurringMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresRecurringMessage.From(reader));
        }

        return results;
    }
}
