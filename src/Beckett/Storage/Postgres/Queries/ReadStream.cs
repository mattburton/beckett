using Beckett.Database;
using Beckett.Database.Models;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadStream(
    string streamName,
    ReadStreamOptions readOptions
) : IPostgresDatabaseQuery<ReadStream.Result>
{
    public async Task<Result> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        //language=sql
        const string sql = """
            WITH stream_version AS (
                SELECT max(stream_position) AS stream_version
                FROM beckett.messages
                WHERE stream_name = $1
                AND archived = false
            ),
            results AS (
                SELECT id,
                       stream_name,
                       stream_position,
                       global_position,
                       type,
                       data,
                       metadata,
                       timestamp
                FROM beckett.messages
                WHERE stream_name = $1
                AND ($2 IS NULL OR stream_position >= $2)
                AND ($3 IS NULL OR stream_position <= $3)
                AND archived = false
                AND ($4 IS NULL OR global_position >= $4)
                AND ($5 IS NULL OR global_position <= $5)
                AND ($6 IS NULL OR type = ANY($6))
                ORDER BY CASE WHEN $7 = true THEN stream_position END,
                         CASE WHEN $7 = false THEN stream_position END DESC
                LIMIT $8
            )
            SELECT coalesce((SELECT stream_version FROM stream_version), 0) AS stream_version,
                   null AS id,
                   null AS stream_name,
                   null AS stream_position,
                   null AS global_position,
                   null AS type,
                   null AS data,
                   null AS metadata,
                   null AS timestamp
            UNION ALL
            SELECT null,
                   id,
                   stream_name,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            FROM results;
        """;

        command.CommandText = Query.Build(nameof(ReadStream), sql, out var prepare);

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

        if (prepare)
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

        var streamVersion = 0L;

        if (await reader.ReadAsync(cancellationToken))
        {
            streamVersion = reader.GetInt64(0);
        }

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresStreamMessage.From(reader));
        }

        return new Result(streamVersion, results);
    }

    public record Result(long StreamVersion, List<PostgresStreamMessage> StreamMessages);
}
