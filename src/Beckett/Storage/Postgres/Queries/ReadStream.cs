using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadStream(
    string streamName,
    ReadStreamOptions readOptions,
    PostgresOptions postgresOptions
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresMessage>>
{
    public async Task<IReadOnlyList<PostgresMessage>> Execute(
        NpgsqlCommand command,
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
            from {postgresOptions.Schema}.read_stream($1, $2, $3, $4, $5, $6, $7);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });
        command.Parameters.Add(
            new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text, IsNullable = true }
        );

        if (postgresOptions.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = readOptions.StartingStreamPosition.HasValue
            ? readOptions.StartingStreamPosition.Value
            : DBNull.Value;
        command.Parameters[2].Value = readOptions.EndingStreamPosition.HasValue
            ? readOptions.EndingStreamPosition.Value
            : DBNull.Value;
        command.Parameters[3].Value = readOptions.StartingGlobalPosition.HasValue
            ? readOptions.StartingGlobalPosition.Value
            : DBNull.Value;
        command.Parameters[4].Value = readOptions.EndingGlobalPosition.HasValue
            ? readOptions.EndingGlobalPosition.Value
            : DBNull.Value;
        command.Parameters[5].Value = readOptions.Count.HasValue ? readOptions.Count.Value : DBNull.Value;
        command.Parameters[6].Value = readOptions.ReadForwards.GetValueOrDefault(true);
        command.Parameters[7].Value = readOptions.Types is { Length: > 0 } ? readOptions.Types : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresMessage.From(reader));
        }

        return results;
    }
}
