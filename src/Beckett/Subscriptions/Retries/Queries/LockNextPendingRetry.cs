using Beckett.Database;
using Beckett.Subscriptions.Retries.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Retries.Queries;

public class LockNextPendingRetry(
    string groupName
) : IPostgresDatabaseQuery<Retry?>
{
    public async Task<Retry?> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select retry_id,
                   group_name,
                   name,
                   stream_name,
                   stream_position,
                   error
            from {schema}.lock_next_pending_retry($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = groupName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Retry(
                reader.GetFieldValue<Guid>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<long>(4),
                ExceptionData.FromJson(reader.GetFieldValue<string>(5))!
            );
    }
}
