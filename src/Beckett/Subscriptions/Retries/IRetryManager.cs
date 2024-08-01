using Npgsql;

namespace Beckett.Subscriptions.Retries;

public interface IRetryManager
{
    Task StartRetry(
        long checkpointId,
        string subscriptionName,
        string streamName,
        long streamPosition,
        string lastError,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    );

    Task RecordFailure(
        long checkpointId,
        string subscriptionName,
        string streamName,
        long streamPosition,
        string lastError,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    );

    Task Retry(
        long checkpointId,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    );


    Task ManualRetry(long checkpointId, CancellationToken cancellationToken);

    Task DeleteRetry(long checkpointId, CancellationToken cancellationToken);
}
