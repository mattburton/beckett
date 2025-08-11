using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class GetGlobalReaderPosition : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT position
            FROM beckett.global_reader_position;
        """;

        command.CommandText = Query.Build(nameof(GetGlobalReaderPosition), sql, out var prepare);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return reader.GetFieldValue<long>(0);
        }

        return 0;
    }
}