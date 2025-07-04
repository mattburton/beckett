using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadGlobalStream(
    ReadGlobalStreamOptions readOptions,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<ReadGlobalStream.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            WITH transaction_id AS (
                SELECT m.transaction_id
                FROM {options.Schema}.messages m
                WHERE m.global_position = $1
                AND m.archived = false
                UNION ALL
                SELECT '0'::xid8
                LIMIT 1
            )
            SELECT m.stream_name,
                   m.stream_position,
                   m.global_position,
                   m.type,
                   m.metadata ->> '$tenant',
                   m.timestamp
            FROM {options.Schema}.messages m
            WHERE (m.transaction_id, m.global_position) > ((SELECT transaction_id FROM transaction_id), $1)
            AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
            AND m.archived = false
            ORDER BY m.transaction_id, m.global_position
            LIMIT $2;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
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
                    reader.GetFieldValue<string>(0),
                    reader.GetFieldValue<long>(1),
                    reader.GetFieldValue<long>(2),
                    reader.GetFieldValue<string>(3),
                    reader.IsDBNull(4) ? null : reader.GetFieldValue<string>(4),
                    reader.GetFieldValue<DateTimeOffset>(5)
                )
            );
        }

        return results;
    }

    public readonly record struct Result(
        string StreamName,
        long StreamPosition,
        long GlobalPosition,
        string MessageType,
        string? Tenant,
        DateTimeOffset Timestamp
    );
}
