using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Retries.Queries;

public class ReserveNextAvailableRetry(
    string groupName,
    TimeSpan reservationTimeout
) : IPostgresDatabaseQuery<ReserveNextAvailableRetry.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id, group_name, name, stream_name, stream_position, status, attempts, max_retry_count, error
            from {schema}.reserve_next_available_retry($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = reservationTimeout;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Result(
                reader.GetFieldValue<Guid>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<long>(4),
                reader.GetFieldValue<RetryStatus>(5),
                reader.IsDBNull(6) ? null : reader.GetFieldValue<int>(6),
                reader.GetFieldValue<int>(7),
                reader.GetFieldValue<string>(8)
            );
    }

    public record Result(
        Guid Id,
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition,
        RetryStatus Status,
        int? Attempts,
        int MaxRetryCount,
        string? Error
    );
}
