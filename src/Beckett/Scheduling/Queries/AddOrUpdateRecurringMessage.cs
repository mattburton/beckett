using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class AddOrUpdateRecurringMessage(
    string name,
    string cronExpression,
    string streamName,
    Message message,
    DateTimeOffset nextOccurrence
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            INSERT INTO beckett.recurring_messages (
                name,
                cron_expression,
                stream_name,
                type,
                data,
                metadata,
                next_occurrence
            )
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            ON CONFLICT (name) DO UPDATE
              SET cron_expression = excluded.cron_expression,
                  stream_name = excluded.stream_name,
                  data = excluded.data,
                  metadata = excluded.metadata,
                  next_occurrence = excluded.next_occurrence;
        """;

        command.CommandText = Query.Build(nameof(AddOrUpdateRecurringMessage), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = name;
        command.Parameters[1].Value = cronExpression;
        command.Parameters[2].Value = streamName;
        command.Parameters[3].Value = message.Type;
        command.Parameters[4].Value = message.Data;
        command.Parameters[5].Value = message.SerializedMetadata;
        command.Parameters[6].Value = nextOccurrence;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
