using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ReadStreamChanges(
    long lastGlobalPosition,
    int batchSize
) : IPostgresDatabaseQuery<IReadOnlyList<ReadStreamChanges.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select topic,
                   stream_id,
                   stream_version,
                   global_position,
                   message_types
            from {schema}.read_stream_changes($1, $2);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = lastGlobalPosition;
        command.Parameters[1].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Result(
                reader.GetFieldValue<string>(0),
                reader.GetFieldValue<string>(1),
                reader.GetFieldValue<long>(2),
                reader.GetFieldValue<long>(3),
                reader.GetFieldValue<string[]>(4)
            ));
        }

        return results;
    }

    public readonly record struct Result(
        string Topic,
        string StreamId,
        long StreamVersion,
        long GlobalPosition,
        string[] MessageTypes
    );
}
