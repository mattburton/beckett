using Beckett.Database;
using Beckett.Database.Types;
using Beckett.MessageStorage.Postgres.Queries;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageStorage(IPostgresDatabase database, PostgresOptions options) : IMessageStorage
{
    public async Task<AppendToStreamResult> AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages,
        CancellationToken cancellationToken
    )
    {
        var newMessages = messages.Select(x => MessageType.From(streamName, x)).ToArray();

        var query = new AppendToStream(streamName, expectedVersion.Value, newMessages, options);

        var streamVersion = await Pipeline.ExecuteAsync(
            static async (state, token) => await state.database.Execute(state.query, token),
            (database, query),
            cancellationToken
        );

        return new AppendToStreamResult(streamVersion);
    }

    public async Task<ReadGlobalStreamResult> ReadGlobalStream(
        long lastGlobalPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var query = new ReadGlobalStream(lastGlobalPosition, batchSize, options);

        var results = await Pipeline.ExecuteAsync(
            static async (state, token) => await state.database.Execute(state.query, token),
            (database, query),
            cancellationToken
        );

        var items = new List<GlobalStreamItem>();

        if (results.Count <= 0)
        {
            return new ReadGlobalStreamResult(items);
        }

        items.AddRange(
            results.Select(
                result => new GlobalStreamItem(
                    result.StreamName,
                    result.StreamPosition,
                    result.GlobalPosition,
                    result.MessageType
                )
            )
        );

        return new ReadGlobalStreamResult(items);
    }

    public async Task<ReadStreamResult> ReadStream(
        string streamName,
        ReadStreamOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var query = new ReadStream(streamName, readOptions, options);

        var streamMessages = await Pipeline.ExecuteAsync(
            static async (state, token) => await state.database.Execute(state.query, token),
            (database, query),
            cancellationToken
        );

        var streamVersion = streamMessages.Count == 0 ? 0 : streamMessages[0].StreamVersion;

        var messages = new List<StreamMessage>();

        foreach (var streamMessage in streamMessages)
        {
            messages.Add(
                new StreamMessage(
                    streamMessage.Id.ToString(),
                    streamMessage.StreamName,
                    streamMessage.StreamPosition,
                    streamMessage.GlobalPosition,
                    streamMessage.Type,
                    streamMessage.Data,
                    streamMessage.Metadata,
                    streamMessage.Timestamp
                )
            );
        }

        return new ReadStreamResult(streamName, streamVersion, messages);
    }

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder().AddRetry(
        new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        }
    ).Build();
}
