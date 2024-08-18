using Beckett.Database.Models;
using Npgsql;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStreamBatchCommand(
    string streamName,
    ReadOptions? readOptions,
    IPostgresMessageDeserializer messageDeserializer,
    AppendToStreamDelegate appendToStream
)
{
    public string StreamName => streamName;
    public ReadOptions? ReadOptions => readOptions;

    public TaskCompletionSource<MessageStream> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<MessageStream> Result => Completion.Task;

    public async Task Read(NpgsqlDataReader reader, CancellationToken cancellationToken)
    {
        var results = new List<PostgresMessage>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(PostgresMessage.From(reader));
        }

        var streamVersion = results.Count == 0 ? 0 : results[^1].StreamPosition;

        var streamMessages = new List<MessageResult>();

        foreach (var result in results)
        {
            var streamMessage = messageDeserializer.Deserialize(result);

            if (streamMessage is null)
            {
                continue;
            }

            streamMessages.Add(streamMessage.Value);
        }

        var stream = new MessageStream(
            streamName,
            streamVersion,
            streamMessages,
            appendToStream
        );

        Completion.SetResult(stream);
    }
}
