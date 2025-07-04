using Beckett.Database;
using Beckett.Database.Models;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadStream(
    string streamName,
    ReadStreamOptions readOptions,
    PostgresOptions postgresOptions
) : IPostgresDatabaseQuery<IReadOnlyList<PostgresStreamMessage>>
{
    public async Task<IReadOnlyList<PostgresStreamMessage>> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"""
            WITH stream_version AS (
                SELECT max(m.stream_position) AS stream_version
                FROM {postgresOptions.Schema}.messages m
                WHERE m.stream_name = $1
                AND m.archived = false
            ),
            stream_version_or_default AS (
                SELECT coalesce(stream_version, 0) AS stream_version
                FROM stream_version
            )
            SELECT m.id,
                   m.stream_name,
                   (SELECT stream_version FROM stream_version_or_default) AS stream_version,
                   m.stream_position,
                   m.global_position,
                   m.type,
                   m.data,
                   m.metadata,
                   m.timestamp
            FROM {postgresOptions.Schema}.messages m
            WHERE m.stream_name = $1
            AND ($2 IS NULL OR m.stream_position >= $2)
            AND ($3 IS NULL OR m.stream_position <= $3)
            AND m.archived = false
            AND ($4 IS NULL OR m.global_position >= $4)
            AND ($5 IS NULL OR m.global_position <= $5)
            AND ($6 IS NULL OR m.type = ANY($6))
            ORDER BY CASE WHEN $7 = true THEN m.stream_position END,
                     CASE WHEN $7 = false THEN m.stream_position END DESC
            LIMIT $8;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
        command.Parameters.Add(
            new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text, IsNullable = true }
        );
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });

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
        command.Parameters[5].Value = readOptions.Types is { Length: > 0 } ? readOptions.Types : DBNull.Value;
        command.Parameters[6].Value = readOptions.ReadForwards.GetValueOrDefault(true);
        command.Parameters[7].Value = readOptions.Count.HasValue ? readOptions.Count.Value : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<PostgresStreamMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresStreamMessage.From(reader));
        }

        return results;
    }
}
