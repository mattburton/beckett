using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class LockCheckpoint(
    string groupName,
    string name,
    string streamName
) : IPostgresDatabaseQuery<LockCheckpoint.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, stream_position
            FROM beckett.checkpoints
            WHERE group_name = $1
            AND name = $2
            AND stream_name = $3
            FOR UPDATE
            SKIP LOCKED;
        """;

        command.CommandText = Query.Build(nameof(LockCheckpoint), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
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
