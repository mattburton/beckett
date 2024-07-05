using Beckett.Subscriptions.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class LockNextCheckpointForRetry(string application) : IPostgresDatabaseQuery<LockNextCheckpointForRetry.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select application, name, stream_name, stream_position, status, last_error, retry_id
            from {schema}.lock_next_checkpoint_for_retry($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Result(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<CheckpointStatus>(4),
                reader.GetFieldValue<string>(5),
                reader.GetFieldValue<Guid>(6)
            );
    }

    public record Result(
        string Application,
        string Name,
        string StreamName,
        long StreamPosition,
        CheckpointStatus Status,
        string LastError,
        Guid RetryId
    );
}
