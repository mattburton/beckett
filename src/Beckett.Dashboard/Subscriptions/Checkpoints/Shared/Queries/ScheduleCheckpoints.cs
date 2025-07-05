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
        const string sql = """
            UPDATE beckett.checkpoints
            SET process_at = $2
            WHERE id = ANY($1);
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
