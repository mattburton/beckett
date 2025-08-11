using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Subscription;

public class EnhancedSubscriptionQuery(
    string groupName,
    string subscriptionName
) : IPostgresDatabaseQuery<EnhancedSubscriptionQuery.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT s.id,
                   sg.name as group_name,
                   s.name as subscription_name,
                   s.status,
                   s.category,
                   s.stream_name,
                   array_agg(mt.name ORDER BY mt.name) FILTER (WHERE mt.name IS NOT NULL) as message_types,
                   s.priority,
                   s.skip_during_replay,
                   s.replay_target_position,
                   coalesce(active.active_count, 0) as active_checkpoints,
                   coalesce(lagging.lagging_count, 0) as lagging_checkpoints,
                   coalesce(failed.failed_count, 0) as failed_checkpoints,
                   coalesce(retry.retry_count, 0) as retry_checkpoints
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            LEFT JOIN beckett.subscription_message_types smt ON s.id = smt.subscription_id
            LEFT JOIN beckett.message_types mt ON smt.message_type_id = mt.id
            LEFT JOIN (
                SELECT subscription_id, count(*) as active_count
                FROM beckett.checkpoints
                WHERE status = 'active'
                GROUP BY subscription_id
            ) active ON s.id = active.subscription_id
            LEFT JOIN (
                SELECT c.subscription_id, count(*) as lagging_count
                FROM beckett.checkpoints c
                LEFT JOIN beckett.checkpoints_ready cr ON c.id = cr.id
                WHERE c.status = 'active' 
                AND c.stream_version > c.stream_position
                AND cr.id IS NOT NULL
                GROUP BY c.subscription_id
            ) lagging ON s.id = lagging.subscription_id
            LEFT JOIN (
                SELECT subscription_id, count(*) as failed_count
                FROM beckett.checkpoints
                WHERE status = 'failed'
                GROUP BY subscription_id
            ) failed ON s.id = failed.subscription_id
            LEFT JOIN (
                SELECT subscription_id, count(*) as retry_count
                FROM beckett.checkpoints
                WHERE status = 'retry'
                GROUP BY subscription_id
            ) retry ON s.id = retry.subscription_id
            WHERE sg.name = $1 AND s.name = $2;
        """;

        command.CommandText = Query.Build(nameof(EnhancedSubscriptionQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = subscriptionName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            var messageTypes = reader.IsDBNull(6) ? new string[0] : reader.GetFieldValue<string[]>(6);

            return new Result(
                reader.GetFieldValue<long>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<string>(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<string>(5),
                messageTypes,
                reader.GetFieldValue<int>(7),
                reader.GetFieldValue<bool>(8),
                reader.IsDBNull(9) ? null : reader.GetFieldValue<long>(9),
                reader.GetFieldValue<long>(10),
                reader.GetFieldValue<long>(11),
                reader.GetFieldValue<long>(12),
                reader.GetFieldValue<long>(13)
            );
        }

        return null;
    }

    public record Result(
        long SubscriptionId,
        string GroupName,
        string SubscriptionName,
        string Status,
        string? Category,
        string? StreamName,
        string[] MessageTypes,
        int Priority,
        bool SkipDuringReplay,
        long? ReplayTargetPosition,
        long ActiveCheckpoints,
        long LaggingCheckpoints,
        long FailedCheckpoints,
        long RetryCheckpoints
    );
}