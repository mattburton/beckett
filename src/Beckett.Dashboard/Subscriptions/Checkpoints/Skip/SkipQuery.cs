using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Skip;

public class SkipQuery(
    long id
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            DELETE FROM beckett.checkpoints_reserved WHERE id = $1;
            DELETE FROM beckett.checkpoints_ready WHERE id = $1;
            
            UPDATE beckett.checkpoints
            SET stream_position = CASE WHEN stream_position + 1 > stream_version THEN stream_position ELSE stream_position + 1 END,
                process_at = NULL,
                reserved_until = NULL,
                status = 'active',
                retry_attempts = 0,
                retries = NULL,
                updated_at = now()
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(SkipQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
