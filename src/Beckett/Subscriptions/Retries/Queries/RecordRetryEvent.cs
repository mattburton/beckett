using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Retries.Queries;

public class RecordRetryEvent(
    Guid retryId,
    RetryStatus status,
    int? attempt = null,
    DateTimeOffset? retryAt = null,
    string? error = null
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.record_retry_event($1, $2, $3, $4, $5);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.RetryStatus(schema) });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Jsonb, IsNullable = true });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = retryId;
        command.Parameters[1].Value = status;
        command.Parameters[2].Value = attempt == null ? DBNull.Value : attempt.Value;
        command.Parameters[3].Value = retryAt == null ? DBNull.Value : retryAt.Value;
        command.Parameters[4].Value = error == null ? DBNull.Value : error;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
