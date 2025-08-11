using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Shared.Queries;

public class ScheduleCheckpoints(
    long[] ids,
    DateTimeOffset processAt
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH target_versions AS (
                SELECT c.id, c.stream_position + 1 as target_stream_version
                FROM beckett.checkpoints c
                INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name
                WHERE c.id = ANY($1)
                AND m.stream_position > c.stream_position
                AND c.status = 'active'
            ),
            updated_checkpoints AS (
                UPDATE beckett.checkpoints
                SET updated_at = now()
                WHERE id = ANY($1)
                RETURNING id, status
            )
            INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
            SELECT uc.id, $2, sg.name, tv.target_stream_version
            FROM updated_checkpoints uc
            INNER JOIN target_versions tv ON uc.id = tv.id
            INNER JOIN beckett.checkpoints c ON uc.id = c.id
            INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            WHERE uc.status = 'active'
            ON CONFLICT (id) DO UPDATE
                SET process_at = EXCLUDED.process_at,
                    target_stream_version = EXCLUDED.target_stream_version;
        """;

        command.CommandText = Query.Build(nameof(ScheduleCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.TimestampTz });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = ids;
        command.Parameters[1].Value = processAt;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
