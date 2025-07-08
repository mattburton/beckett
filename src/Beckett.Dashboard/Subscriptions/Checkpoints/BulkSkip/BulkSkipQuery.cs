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
            WITH release_reservation AS (
                DELETE FROM beckett.checkpoints_reserved
                WHERE id = ANY($1)
            ),
            clear_ready AS (
                DELETE FROM beckett.checkpoints_ready
                WHERE id = ANY($1)
            )
            UPDATE beckett.checkpoints
            SET stream_position = stream_position + 1,
                status = 'active',
                retries = NULL,
                updated_at = now()
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
