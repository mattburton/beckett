using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadGlobalStream(
    ReadGlobalStreamOptions readOptions
) : IPostgresDatabaseQuery<IReadOnlyList<ReadGlobalStream.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH transaction_id AS (
                SELECT m.transaction_id
                FROM beckett.messages m
                WHERE m.global_position = $1
                AND m.archived = false
                UNION ALL
                SELECT '0'::xid8
                LIMIT 1
            )
            SELECT m.id,
                   m.stream_name,
                   m.stream_position,
                   m.global_position,
                   m.type,
                   m.metadata ->> '$tenant',
                   m.metadata ->> '$correlation_id',
                   m.timestamp
            FROM beckett.messages m
            WHERE (m.transaction_id, m.global_position) > ((SELECT transaction_id FROM transaction_id), $1)
            AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
            AND m.archived = false
            ORDER BY m.transaction_id, m.global_position
            LIMIT $2;
        """;

        command.CommandText = Query.Build(nameof(ReadGlobalStream), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = readOptions.LastGlobalPosition;
        command.Parameters[1].Value = readOptions.BatchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(
                new Result(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<long>(2),
                    reader.GetFieldValue<long>(3),
                    reader.GetFieldValue<string>(4),
                    reader.IsDBNull(5) ? null : reader.GetFieldValue<string>(5),
                    reader.IsDBNull(6) ? null : reader.GetFieldValue<string>(6),
                    reader.GetFieldValue<DateTimeOffset>(7)
                )
            );
        }

        return results;
    }

    public readonly record struct Result(
        Guid Id,
        string StreamName,
        long StreamPosition,
        long GlobalPosition,
        string MessageType,
        string? Tenant,
        string? CorrelationId,
        DateTimeOffset Timestamp
    );
}
