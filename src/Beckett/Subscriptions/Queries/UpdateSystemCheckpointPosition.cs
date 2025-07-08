using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateSystemCheckpointPosition(
    long id,
    long position
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.checkpoints
            SET stream_position = $2,
                updated_at = now()
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(UpdateSystemCheckpointPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = position;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
