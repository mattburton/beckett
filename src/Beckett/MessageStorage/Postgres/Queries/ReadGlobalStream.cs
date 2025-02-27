using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres.Queries;

public class ReadGlobalStream(
    ReadGlobalStreamOptions readOptions,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresMessage>>
{
    public async Task<IReadOnlyList<PostgresMessage>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select id,
                   stream_name,
                   0 as stream_version,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            from {options.Schema}.read_global_stream($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = readOptions.StartingGlobalPosition;
        command.Parameters[1].Value = readOptions.EndingGlobalPosition.HasValue
            ? readOptions.EndingGlobalPosition.Value
            : DBNull.Value;
        command.Parameters[2].Value = readOptions.Count;
        command.Parameters[3].Value = readOptions.Types is { Length: > 0 } ? readOptions.Types : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresMessage.From(reader));
        }

        return results;
    }
}
