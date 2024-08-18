using Beckett.Database;
using Beckett.OpenTelemetry;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStreamBatch(
    BeckettOptions options,
    IPostgresDatabase database,
    IPostgresMessageDeserializer messageDeserializer,
    IInstrumentation instrumentation,
    AppendToStreamDelegate appendToStream
) : IMessageStreamBatch
{
    private readonly List<PostgresMessageStreamBatchCommand> _commands = [];

    public Task<MessageStream> ReadStream(string streamName, ReadOptions? readOptions = null)
    {
        var command = new PostgresMessageStreamBatchCommand(
            streamName,
            readOptions,
            messageDeserializer,
            appendToStream
        );

        _commands.Add(command);

        return command.Result;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        using var activity = instrumentation.StartReadStreamBatchActivity();

        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var batch = new NpgsqlBatch(connection);

        foreach (var item in _commands)
        {
            var command = batch.CreateBatchCommand();

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
                from {options.Postgres.Schema}.read_stream($1, $2, $3, $4, $5, $6, $7);
            ";

            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint, IsNullable = true });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, IsNullable = true });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

            var readOptions = item.ReadOptions ?? ReadOptions.Default;

            command.Parameters[0].Value = item.StreamName;
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

            batch.BatchCommands.Add(command);
        }

        await using var reader = await batch.ExecuteReaderAsync(cancellationToken);

        foreach (var command in _commands)
        {
            await command.Read(reader, cancellationToken);

            await reader.NextResultAsync(cancellationToken);
        }
    }
}
