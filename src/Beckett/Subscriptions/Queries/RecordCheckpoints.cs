using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH checkpoints_to_record AS (
                SELECT c.group_name, c.name, c.stream_name, c.stream_position
                FROM unnest($1) AS c
            ),
            record_checkpoints AS (
                INSERT INTO beckett.checkpoints (group_name, name, stream_name, stream_position)
                SELECT c.group_name, c.name, c.stream_name, c.stream_position
                FROM checkpoints_to_record c
                ON CONFLICT (group_name, name, stream_name) DO NOTHING
                RETURNING id
            ),
            checkpoint_ids AS (
                SELECT r.id
                FROM record_checkpoints AS r
                UNION ALL
                SELECT c.id
                FROM checkpoints_to_record cr
                INNER JOIN beckett.checkpoints c ON cr.group_name = c.group_name AND
                                                    cr.name = c.name AND
                                                    cr.stream_name = c.stream_name
            ),
            insert_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
                SELECT c.id, c.group_name, now()
                FROM beckett.checkpoints AS c
                INNER JOIN checkpoint_ids AS ci on c.id = ci.id
                ON CONFLICT (id) DO NOTHING
                RETURNING group_name
            )
            SELECT pg_notify('beckett:checkpoints', group_name)
            FROM insert_ready;
        """;

        command.CommandText = Query.Build(nameof(RecordCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray("beckett") });
        command.Parameters.Add(
            new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text }
        );

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpoints;
        command.Parameters[1].Value = checkpoints.Select(x => x.GroupName).Distinct().ToArray();

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
