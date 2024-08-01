using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class LockNextCheckpointForRetry(string subscriptionGroupName) : IPostgresDatabaseQuery<LockNextCheckpointForRetry.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id, name, stream_name, stream_position, status, last_error
            from {schema}.lock_next_checkpoint_for_retry($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = subscriptionGroupName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Result(
                reader.GetFieldValue<long>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<CheckpointStatus>(4),
                reader.GetFieldValue<string>(5)
            );
    }

    public record Result(
        long Id,
        string Name,
        string StreamName,
        long StreamPosition,
        CheckpointStatus Status,
        string LastError
    );
}
