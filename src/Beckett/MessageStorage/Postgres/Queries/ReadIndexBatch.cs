using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres.Queries;

public class ReadIndexBatch(
    ReadIndexBatchOptions readOptions,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<ReadIndexBatch.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select stream_name, stream_position, global_position, type, tenant, timestamp
            from {options.Schema}.read_index_batch($1, $2, $3, $4);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(
            new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text, IsNullable = true }
        );

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = readOptions.StartingGlobalPosition;
        command.Parameters[1].Value = readOptions.BatchSize;
        command.Parameters[2].Value =
            string.IsNullOrWhiteSpace(readOptions.Category) ? DBNull.Value : readOptions.Category;
        command.Parameters[3].Value = readOptions.Types is { Length: > 0 } ? readOptions.Types : DBNull.Value;

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
