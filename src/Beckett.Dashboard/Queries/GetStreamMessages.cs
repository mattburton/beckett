using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Queries;

public class GetStreamMessages(string streamName) : IPostgresDatabaseQuery<IReadOnlyList<GetStreamMessages.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select id, stream_position, type, timestamp
            from {schema}.messages
            where stream_name = $1
            order by stream_position;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetFieldValue<int>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<DateTimeOffset>(3)
                )
            );
        }

        return results;
    }

    public record Result(Guid Id, int StreamPosition, string Type, DateTimeOffset Timestamp);
}
