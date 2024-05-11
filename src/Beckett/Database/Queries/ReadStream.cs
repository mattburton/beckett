using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ReadStream(
    string topic,
    string streamId,
    ReadOptions options
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
                   topic,
                   stream_id,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            from {schema}.read_stream($1, $2, $3, $4, $5, $6);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = topic;
        command.Parameters[1].Value = streamId;
        command.Parameters[2].Value = options.StartingStreamPosition.HasValue
            ? options.StartingStreamPosition.Value
            : DBNull.Value;
        command.Parameters[3].Value = options.EndingGlobalPosition.HasValue
            ? options.EndingGlobalPosition.Value
            : DBNull.Value;
        command.Parameters[4].Value = options.Count.HasValue ? options.Count.Value : DBNull.Value;
        command.Parameters[5].Value = options.ReadForwards.GetValueOrDefault(true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresMessage.From(reader));
        }

        return results;
    }
}
