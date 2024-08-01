using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ReserveNextAvailableCheckpoint(string groupName, TimeSpan reservationTimeout)
    : IPostgresDatabaseQuery<Checkpoint?>
{
    public async Task<Checkpoint?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id, group_name, name, stream_name, stream_position, stream_version, status
            from {schema}.reserve_next_available_checkpoint($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = reservationTimeout;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return Checkpoint.From(reader);
    }
}
