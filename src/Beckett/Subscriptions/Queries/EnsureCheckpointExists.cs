using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class EnsureCheckpointExists(
    long subscriptionId,
    string streamName
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH new_checkpoint AS (
                INSERT INTO beckett.checkpoints (subscription_id, stream_name)
                VALUES ($1, $2)
                ON CONFLICT (subscription_id, stream_name) DO NOTHING
                RETURNING 0 as stream_position
            )
            SELECT stream_position
            FROM beckett.checkpoints
            WHERE subscription_id = $1
            AND stream_name = $2
            UNION ALL
            SELECT stream_position
            FROM new_checkpoint;
        """;

        command.CommandText = Query.Build(nameof(EnsureCheckpointExists), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;
        command.Parameters[1].Value = streamName;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result switch
        {
            long id => id,
            _ => throw new Exception($"Unexpected result from ensure_checkpoint_exists function: {result}")
        };
    }
}
