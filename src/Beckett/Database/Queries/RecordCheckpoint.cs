using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class RecordCheckpoint(
    string name,
    string streamName,
    long streamPosition,
    long streamVersion
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.record_checkpoint($1, $2, $3, $4);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = name;
        command.Parameters[1].Value = streamName;
        command.Parameters[2].Value = streamPosition;
        command.Parameters[3].Value = streamVersion;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
