using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class RecordCheckpoint(
    string application,
    string name,
    string topic,
    string streamId,
    long streamPosition,
    long streamVersion
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.record_checkpoint($1, $2, $3, $4, $5, $6);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = topic;
        command.Parameters[3].Value = streamId;
        command.Parameters[4].Value = streamPosition;
        command.Parameters[5].Value = streamVersion;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
