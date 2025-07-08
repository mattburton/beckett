using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class EnsureCheckpointExists(
    string groupName,
    string name,
    string streamName
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            INSERT INTO beckett.checkpoints (group_name, name, stream_name)
            VALUES ($1, $2, $3)
            ON CONFLICT (group_name, name, stream_name) DO NOTHING;
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

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
