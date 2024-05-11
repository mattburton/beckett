using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class LockCheckpoint(
    string application,
    string name,
    string topic,
    string streamId
) : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select application,
                   name,
                   topic,
                   stream_id,
                   stream_position,
                   stream_version,
                   blocked
            from {schema}.lock_checkpoint($1, $2, $3, $4);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = topic;
        command.Parameters[3].Value = streamId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows ? null : new Checkpoint(
            reader.GetFieldValue<string>(0),
            reader.GetFieldValue<string>(1),
            reader.GetFieldValue<string>(2),
            reader.GetFieldValue<string>(3),
            reader.GetFieldValue<long>(4),
            reader.GetFieldValue<long>(5),
            reader.GetFieldValue<bool>(6)
        );
    }
}
