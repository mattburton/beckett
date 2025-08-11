using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetStreamTypesForStream(
    string streamName
) : IPostgresDatabaseQuery<IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT message_type
            FROM beckett.stream_types
            WHERE stream_name = $1
            ORDER BY message_type;
        """;

        command.CommandText = Query.Build(nameof(GetStreamTypesForStream), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetFieldValue<string>(0));
        }

        return results;
    }
}