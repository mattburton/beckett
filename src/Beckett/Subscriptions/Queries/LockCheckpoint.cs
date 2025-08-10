using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class LockCheckpoint(
    long subscriptionId,
    string streamName
) : IPostgresDatabaseQuery<LockCheckpoint.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT id, stream_position
            FROM beckett.checkpoints
            WHERE subscription_id = $1
            AND stream_name = $2
            FOR UPDATE
            SKIP LOCKED;
        """;

        command.CommandText = Query.Build(nameof(LockCheckpoint), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;
        command.Parameters[1].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows ? null : new Result(reader.GetFieldValue<long>(0), reader.GetFieldValue<long>(1));
    }

    public record Result(long Id, long StreamPosition);
}
