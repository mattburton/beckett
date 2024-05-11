using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class EnsureCheckpointExists(
    string application,
    string name,
    string topic,
    string streamId
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.ensure_checkpoint_exists($1, $2, $3, $4);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = topic;
        command.Parameters[3].Value = streamId;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
