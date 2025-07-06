using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.BulkSkip;

public class BulkSkipQuery(
    long[] ids
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.checkpoints
            SET stream_position = CASE WHEN stream_position + 1 > stream_version THEN stream_position ELSE stream_position + 1 END,
                process_at = NULL,
                reserved_until = NULL,
                status = 'active',
                retries = NULL
            WHERE id = ANY($1);
        """;

        command.CommandText = Query.Build(nameof(BulkSkipQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = ids;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
