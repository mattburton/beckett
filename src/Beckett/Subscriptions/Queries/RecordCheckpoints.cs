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
            WITH checkpoint_data AS (
                SELECT c.stream_position, c.subscription_id, c.stream_name, c.stream_version
                FROM unnest($1) c
            ),
            updated_checkpoints AS (
                INSERT INTO beckett.checkpoints (stream_position, subscription_id, stream_name, updated_at)
                SELECT cd.stream_position, cd.subscription_id, cd.stream_name, now()
                FROM checkpoint_data cd
                ON CONFLICT (subscription_id, stream_name) DO UPDATE
                    SET updated_at = now()
                RETURNING id, stream_position, status, subscription_id
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
                SELECT uc.id, now(), sg.name, cd.stream_version
                FROM updated_checkpoints uc
                INNER JOIN beckett.subscriptions s ON uc.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                INNER JOIN checkpoint_data cd ON cd.subscription_id = uc.subscription_id AND cd.stream_name = (SELECT stream_name FROM beckett.checkpoints WHERE id = uc.id)
                WHERE uc.status = 'active'
                AND cd.stream_version > uc.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now(),
                        target_stream_version = EXCLUDED.target_stream_version
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', uc.subscription_id::text)
            FROM updated_checkpoints uc
            INNER JOIN checkpoint_data cd ON cd.subscription_id = uc.subscription_id
            WHERE uc.status = 'active' AND cd.stream_version > uc.stream_position;
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
