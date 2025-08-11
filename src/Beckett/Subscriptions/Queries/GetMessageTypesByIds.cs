using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetMessageTypesByIds(long[] messageTypeIds) : IPostgresDatabaseQuery<IReadOnlyList<GetMessageTypesByIds.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT id, name
            FROM beckett.message_types
            WHERE id = ANY($1)
            ORDER BY name;
        """;

        command.CommandText = Query.Build(nameof(GetMessageTypesByIds), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = messageTypeIds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1)
                )
            );
        }

        return results;
    }

    public record Result(long Id, string Name);
}