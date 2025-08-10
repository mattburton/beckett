using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AdvanceLaggingSubscriptionCheckpoints(long subscriptionId) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.checkpoints
            SET stream_position = stream_version,
                process_at = null
            WHERE id IN (
                SELECT id
                FROM beckett.checkpoints
                WHERE subscription_id = $1
                AND lagging = true
                LIMIT 500
            );
        """;

        command.CommandText = Query.Build(nameof(AdvanceLaggingSubscriptionCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
