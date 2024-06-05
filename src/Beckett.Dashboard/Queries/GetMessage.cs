using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Queries;

public class GetMessage(Guid id) : IPostgresDatabaseQuery<GetMessage.Result?>
{
    public async Task<Result?> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select {schema}.stream_category(stream_name) as category, stream_name, type, data, metadata
            from {schema}.messages
            where id = $1
            order by stream_position;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows
            ? null
            : new Result(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<string>(2),
                reader.GetFieldValue<string>(3),
                reader.GetFieldValue<string>(4)
            );
    }

    public record Result(
        string Category,
        string StreamName,
        string Type,
        string Data,
        string Metadata
    );
}
