using Npgsql;

namespace Beckett.Subscriptions;

public interface ISubscriptionStreamProcessor
{
    Task Process(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Subscription subscription,
        string streamName,
        long streamPosition,
        int batchSize,
        bool retryOnError,
        CancellationToken cancellationToken
    );
}
