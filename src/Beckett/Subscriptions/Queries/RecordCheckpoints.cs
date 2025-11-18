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
            WITH upsert_checkpoints AS (
                INSERT INTO beckett.checkpoints (stream_version, stream_position, group_name, name, stream_name)
                SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
                FROM unnest($1) c
                ON CONFLICT (group_name, name, stream_name) DO UPDATE
                    SET stream_version = EXCLUDED.stream_version,
                        updated_at = EXCLUDED.updated_at
                RETURNING id, group_name, name
            )
            INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name)
            SELECT id, group_name, name
            FROM upsert_checkpoints
            ON CONFLICT (checkpoint_id) DO UPDATE
                SET process_at = EXCLUDED.process_at;
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
