using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres.Queries;

public class ReadGlobalStreamCheckpointData(
    long lastGlobalPosition,
    int batchSize,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<ReadGlobalStreamCheckpointData.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select stream_name, stream_position, global_position, type, tenant, timestamp
            from {options.Schema}.read_global_stream_checkpoint_data($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = lastGlobalPosition;
        command.Parameters[1].Value = batchSize;

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
