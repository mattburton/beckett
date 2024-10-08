using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class AddOrUpdateRecurringMessage(
    string name,
    string cronExpression,
    string streamName,
    Message message,
    DateTimeOffset nextOccurrence,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.add_or_update_recurring_message($1, $2, $3, $4, $5, $6, $7);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        if (options.PrepareStatements)
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
