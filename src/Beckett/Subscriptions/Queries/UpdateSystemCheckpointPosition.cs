using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateSystemCheckpointPosition(
    long id,
    long position,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            UPDATE {options.Schema}.checkpoints
            SET stream_version = $2,
                stream_position = $2
            WHERE id = $1;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = position;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
