using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetStreamsByMessageType(
    string messageType
) : IPostgresDatabaseQuery<IReadOnlyList<GetStreamsByMessageType.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT DISTINCT si.stream_name, si.latest_position
            FROM beckett.stream_message_types smt
            INNER JOIN beckett.stream_index si ON smt.stream_index_id = si.id
            INNER JOIN beckett.message_types mt ON smt.message_type_id = mt.id
            WHERE mt.name = $1
            ORDER BY si.stream_name;
        """;

        command.CommandText = Query.Build(nameof(GetStreamsByMessageType), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = messageType;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<long>(1)
                )
            );
        }

        return results;
    }

    public record Result(
        string StreamName,
        long LatestPosition
    );
}