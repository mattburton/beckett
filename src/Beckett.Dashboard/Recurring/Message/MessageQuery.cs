using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Recurring.Message;

public class MessageQuery(string name) : IPostgresDatabaseQuery<PostgresRecurringMessage?>
{
    public async Task<PostgresRecurringMessage?> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        //language=sql
        const string sql = """
            SELECT name, cron_expression, stream_name, type, data, metadata, next_occurrence, timestamp
            FROM beckett.recurring_messages
            WHERE name = $1;
        """;

        const string key = "Recurring.Message.Query";

        command.CommandText = Query.Build(key, sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = name;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return PostgresRecurringMessage.From(reader);
        }

        return null;
    }
}
