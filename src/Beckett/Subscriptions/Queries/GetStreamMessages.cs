using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetStreamMessages(
    IReadOnlyList<long> streamIndexIds,
    int batchSize
) : IPostgresDatabaseQuery<IReadOnlyList<GetStreamMessages.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT mi.id,
                   si.stream_name,
                   mi.stream_position,
                   mi.global_position,
                   mt.name as message_type,
                   t.name as tenant,
                   mi.correlation_id,
                   mi.timestamp
            FROM beckett.message_index_active mi
            INNER JOIN beckett.stream_index si ON mi.stream_index_id = si.id
            INNER JOIN beckett.message_types mt ON mi.message_type_id = mt.id
            LEFT JOIN beckett.tenants t ON mi.tenant_id = t.id
            WHERE mi.stream_index_id = ANY($1)
            ORDER BY mi.global_position
            LIMIT $2;
        """;

        command.CommandText = Query.Build(nameof(GetStreamMessages), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = streamIndexIds.ToArray();
        command.Parameters[1].Value = batchSize;

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
