using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class LockCheckpoint(string name, string streamName) : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select name,
                   stream_name,
                   stream_position,
                   stream_version,
                   blocked
            from {schema}.lock_checkpoint($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = name;
        command.Parameters[1].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows ? null : new Checkpoint(
            reader.GetFieldValue<string>(0),
            reader.GetFieldValue<string>(1),
            reader.GetFieldValue<long>(2),
            reader.GetFieldValue<long>(3),
            reader.GetFieldValue<bool>(4)
        );
    }
}