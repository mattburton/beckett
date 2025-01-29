using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class LockCheckpoint(
    int subscriptionId,
    string streamName,
    PostgresOptions options
) : IPostgresDatabaseQuery<LockCheckpoint.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select id, stream_position from {options.Schema}.lock_checkpoint($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
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
