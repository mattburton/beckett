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
            new_checkpoints AS (
                INSERT INTO beckett.checkpoints (stream_position, subscription_id, stream_name, updated_at)
                SELECT cd.stream_position, cd.subscription_id, cd.stream_name, now()
                FROM checkpoint_data cd
                ON CONFLICT (subscription_id, stream_name) DO NOTHING
                RETURNING id, stream_position, status, subscription_id
            ),
            all_checkpoints AS (
                SELECT c.id, c.stream_position, c.status, c.subscription_id, cd.stream_version
                FROM checkpoint_data cd
                INNER JOIN beckett.checkpoints c ON cd.subscription_id = c.subscription_id AND cd.stream_name = c.stream_name
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
                SELECT ac.id, now(), sg.name, ac.stream_version
                FROM all_checkpoints ac
                INNER JOIN beckett.subscriptions s ON ac.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE ac.status = 'active'
                AND ac.stream_version > ac.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now(),
                        target_stream_version = EXCLUDED.target_stream_version
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', ac.subscription_id::text)
            FROM all_checkpoints ac
            WHERE ac.status = 'active' AND ac.stream_version > ac.stream_position;
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
