using Beckett.Subscriptions.Models;
using Npgsql;

namespace Beckett.Database.Queries;

public class LockNextAvailableCheckpoint : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select name, stream_name, stream_position, stream_version, blocked
            from {schema}.lock_next_available_checkpoint();
        ";

        await command.PrepareAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Checkpoint(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<long>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<bool>(4)
            );
    }
}
