using Npgsql;

namespace Beckett.Subscriptions;

public interface ISubscriptionStreamProcessor
{
    Task Process(
        NpgsqlConnection connection,
        Subscription subscription,
        long checkpointId,
        string streamName,
        long streamPosition,
        int batchSize,
        bool throwOnError,
        CancellationToken cancellationToken
    );
}
