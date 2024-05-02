using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class ReadStreamQuery
{
    public static async Task<IReadOnlyList<StreamEvent>> Execute(
        NpgsqlConnection connection,
        string schema,
        string streamName,
        ReadOptions options,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $@"
            select id,
                   stream_name,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            from {schema}.read_stream($1, $2, $3, $4, $5);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = options.StartingStreamPosition.HasValue
            ? options.StartingStreamPosition.Value
            : DBNull.Value;
        command.Parameters[2].Value = options.EndingGlobalPosition.HasValue
            ? options.EndingGlobalPosition.Value
            : DBNull.Value;
        command.Parameters[3].Value = options.Count.HasValue ? options.Count.Value : DBNull.Value;
        command.Parameters[4].Value = options.ReadForwards.GetValueOrDefault(true);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<StreamEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(StreamEvent.From(reader));
        }

        return results;
    }
}
