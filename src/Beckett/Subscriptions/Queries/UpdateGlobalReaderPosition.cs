using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpdateGlobalReaderPosition(
    long position
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.global_reader_checkpoint
            SET position = $1,
                updated_at = now()
            WHERE id = 1;
        """;

        command.CommandText = Query.Build(nameof(UpdateGlobalReaderPosition), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = position;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}