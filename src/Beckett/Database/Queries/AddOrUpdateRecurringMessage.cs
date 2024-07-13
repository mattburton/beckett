using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class AddOrUpdateRecurringMessage(
    string name,
    string cronExpression,
    string streamName,
    string type,
    string data,
    string metadata,
    DateTimeOffset nextOccurrence
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.add_or_update_recurring_message($1, $2, $3, $4, $5, $6, $7);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = name;
        command.Parameters[1].Value = cronExpression;
        command.Parameters[2].Value = streamName;
        command.Parameters[3].Value = type;
        command.Parameters[4].Value = data;
        command.Parameters[5].Value = metadata;
        command.Parameters[6].Value = nextOccurrence;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
