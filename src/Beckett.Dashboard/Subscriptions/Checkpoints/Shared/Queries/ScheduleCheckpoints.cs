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
                SELECT c.id, c.stream_position + 1 as target_stream_version, c.status, c.subscription_id
                FROM beckett.checkpoints c
                INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name
                WHERE c.id = ANY($1)
                AND m.stream_position > c.stream_position
                AND c.status = 'active'
            )
            INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
            SELECT tv.id, $2, sg.name, tv.target_stream_version
            FROM target_versions tv
            INNER JOIN beckett.subscriptions s ON tv.subscription_id = s.id
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
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
