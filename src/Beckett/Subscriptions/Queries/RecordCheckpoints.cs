using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH updated_checkpoints AS (
                INSERT INTO beckett.checkpoints (stream_version, stream_position, subscription_id, stream_name, process_at)
                SELECT c.stream_version, c.stream_position, c.subscription_id, c.stream_name,
                       CASE WHEN c.stream_version > c.stream_position THEN now() END
                FROM unnest($1) c
                ON CONFLICT (subscription_id, stream_name) DO UPDATE
                    SET stream_version = excluded.stream_version,
                        updated_at = now(),
                        process_at = CASE WHEN excluded.stream_version > checkpoints.stream_position THEN now() END
                RETURNING id, stream_version, stream_position, status, process_at, subscription_id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name)
                SELECT uc.id, uc.process_at, sg.name
                FROM updated_checkpoints uc
                INNER JOIN beckett.subscriptions s ON uc.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE uc.status = 'active'
                AND uc.process_at IS NOT NULL
                AND uc.stream_version > uc.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = EXCLUDED.process_at
                RETURNING id, process_at
            )
            SELECT pg_notify('beckett:checkpoints', uc.subscription_id::text)
            FROM updated_checkpoints uc
            WHERE uc.process_at IS NOT NULL;
        """;

        command.CommandText = Query.Build(nameof(RecordCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray() });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpoints;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
