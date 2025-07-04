using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class LockCheckpoint(
    string groupName,
    string name,
    string streamName,
    PostgresOptions options
) : IPostgresDatabaseQuery<LockCheckpoint.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            SELECT id, stream_position
            FROM {options.Schema}.checkpoints
            WHERE group_name = $1
            AND name = $2
            AND stream_name = $3
            FOR UPDATE
            SKIP LOCKED;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows ? null : new Result(reader.GetFieldValue<long>(0), reader.GetFieldValue<long>(1));
    }

    public record Result(long Id, long StreamPosition);
}
