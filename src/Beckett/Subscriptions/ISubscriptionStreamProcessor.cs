using Npgsql;

namespace Beckett.Subscriptions;

public interface ISubscriptionStreamProcessor
{
    Task Process(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Subscription subscription,
        string topic,
        string streamId,
        long streamPosition,
        int batchSize,
        bool retryOnError,
        CancellationToken cancellationToken
    );
}
