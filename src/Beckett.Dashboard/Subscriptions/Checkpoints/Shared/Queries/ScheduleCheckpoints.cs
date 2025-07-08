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
            INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
            SELECT id, group_name, $2
            FROM beckett.checkpoints
            WHERE id = ANY($1)
            ON CONFLICT (id) DO NOTHING;
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
