using Beckett.Database.Models;
using Beckett.Messages.Storage;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ReadStream(
    string streamName,
    ReadStreamOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresMessage>>
{
    public async Task<IReadOnlyList<PostgresMessage>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select id,
                   stream_name,
                   stream_version,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            from {schema}.read_stream($1, $2, $3, $4);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = options.StartingStreamPosition.HasValue
            ? options.StartingStreamPosition.Value
            : DBNull.Value;
        command.Parameters[2].Value = options.Count.HasValue ? options.Count.Value : DBNull.Value;
        command.Parameters[3].Value = options.ReadForwards.GetValueOrDefault(true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresMessage.From(reader));
        }

        return results;
    }
}
