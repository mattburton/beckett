using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class ScheduleMessage(
    string streamName,
    ScheduledMessageType scheduledMessage,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            INSERT INTO {options.Schema}.scheduled_messages (
              id,
              stream_name,
              type,
              data,
              metadata,
              deliver_at
            )
            VALUES (
              $1,
              $2,
              $3,
              $4,
              $5,
              $6
            )
            ON CONFLICT (id) DO NOTHING;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = scheduledMessage.Id;
        command.Parameters[1].Value = streamName;
        command.Parameters[2].Value = scheduledMessage.Type;
        command.Parameters[3].Value = scheduledMessage.Data;
        command.Parameters[4].Value = scheduledMessage.Metadata;
        command.Parameters[5].Value = scheduledMessage.DeliverAt;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
