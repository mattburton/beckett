using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Messages;
using Beckett.OpenTelemetry;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStoreSession(
    BeckettOptions options,
    IPostgresDatabase database,
    IMessageSerializer messageSerializer,
    IInstrumentation instrumentation
) : IMessageStoreSession
{
    private readonly Dictionary<string, MessageStreamSession> _streams = new();

    public MessageStreamSession AppendToStream(string streamName, ExpectedVersion expectedVersion)
    {
        if (!_streams.TryGetValue(streamName, out var stream))
        {
            _streams.Add(streamName, stream = new MessageStreamSession(streamName, expectedVersion, instrumentation));
        }

        return stream;
    }

    public async Task SaveChanges(CancellationToken cancellationToken)
    {
        if (_streams.Count == 0)
        {
            return;
        }

        try
        {
            using var activity = instrumentation.StartSessionSaveChangesActivity();

            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            var batch = connection.CreateBatch();

            foreach (var stream in _streams)
            {
                var command = new NpgsqlBatchCommand
                {
                    CommandText = $"select {options.Postgres.Schema}.append_to_stream($1, $2, $3);"
                };

                var newMessages = stream.Value.Messages.Select(
                    x => MessageType.From(stream.Key, x.Message, x.Metadata, messageSerializer)
                ).ToArray();

                command.Parameters.Add(
                    new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = stream.Value.StreamName }
                );
                command.Parameters.Add(
                    new NpgsqlParameter
                    {
                        NpgsqlDbType = NpgsqlDbType.Bigint,
                        Value = stream.Value.ExpectedVersion.Value
                    }
                );
                command.Parameters.Add(
                    new NpgsqlParameter
                    {
                        DataTypeName = DataTypeNames.MessageArray(options.Postgres.Schema),
                        Value = newMessages
                    }
                );

                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e)
        {
            e.HandleAppendToStreamError();

            throw;
        }
        finally
        {
            _streams.Clear();
        }
    }
}
