using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class LockCheckpoint(
    string groupName,
    string name,
    string streamName
) : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id,
                   group_name,
                   name,
                   stream_name,
                   stream_position,
                   stream_version,
                   status
            from {schema}.lock_checkpoint($1, $2, $3);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return Checkpoint.From(reader);
    }
}
