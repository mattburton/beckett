using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class EnsureCheckpointExists(
    string groupName,
    string name,
    string streamName
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH new_checkpoint AS (
                INSERT INTO beckett.checkpoints (group_name, name, stream_name)
                VALUES ($1, $2, $3)
                ON CONFLICT (group_name, name, stream_name) DO NOTHING
                RETURNING 0 as stream_version
            )
            SELECT stream_version
            FROM beckett.checkpoints
            WHERE group_name = $1
            AND name = $2
            AND stream_name = $3
            UNION ALL
            SELECT stream_version
            FROM new_checkpoint;
        """;

        command.CommandText = Query.Build(nameof(EnsureCheckpointExists), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = streamName;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result switch
        {
            long id => id,
            _ => throw new Exception($"Unexpected result from ensure_checkpoint_exists function: {result}")
        };
    }
}
