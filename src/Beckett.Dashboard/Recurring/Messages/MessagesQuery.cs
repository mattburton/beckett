using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Recurring.Messages;

public class MessagesQuery(string? query = null, int? page = null, int? pageSize = null)
    : IPostgresDatabaseQuery<MessagesQuery.Result>
{
    public record Result(
        IReadOnlyList<PostgresRecurringMessage> RecurringMessages,
        int TotalResults
    );

    public async Task<Result> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        var actualPage = page.ToPageParameter();
        var actualPageSize = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(actualPage, actualPageSize);

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
                timestamp,
                count(*) over() AS total_results
            FROM beckett.recurring_messages
            WHERE (
                $3 IS NULL
                OR
                (name ILIKE '%' || $3 || '%' OR stream_name ILIKE '%' || $3 || '%' OR type ILIKE '%' || $3 || '%')
            )
            ORDER BY name
            LIMIT $1 OFFSET $2;
        """;

        const string key = "Recurring.Messages.Query";

        command.CommandText = Query.Build(key, sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = actualPageSize;
        command.Parameters[1].Value = offset;
        command.Parameters[2].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;

        var recurringMessages = new List<PostgresRecurringMessage>();

        int? totalResults = null;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(9);

            recurringMessages.Add(PostgresRecurringMessage.From(reader));
        }

        return new Result(recurringMessages, totalResults.GetValueOrDefault(0));
    }
}
