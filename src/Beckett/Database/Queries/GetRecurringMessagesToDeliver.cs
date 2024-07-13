using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class GetRecurringMessagesToDeliver(
    int batchSize
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresRecurringMessage>>
{
    public async Task<IReadOnlyList<PostgresRecurringMessage>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select name,
                   cron_expression,
                   stream_name,
                   type,
                   data,
                   metadata,
                   next_occurrence,
                   timestamp
            from {schema}.get_recurring_messages_to_deliver($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

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
