using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries;

public class RetryManager(
    IPostgresDatabase database,
    ISubscriptionRegistry subscriptionRegistry,
    BeckettOptions options,
    ISubscriptionStreamProcessor subscriptionStreamProcessor,
    IMessageStore messageStore,
    IMessageScheduler messageScheduler
) : IRetryManager
{
    public async Task StartRetry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        await messageScheduler.ScheduleMessage(
            RetryStreamName.For(id),
            new RetryStarted(
                id,
                options.ApplicationName,
                subscriptionName,
                streamName,
                streamPosition,
                DateTimeOffset.UtcNow
            ),
            0.GetNextDelayWithExponentialBackoff(),
            cancellationToken
        );
    }

    public async Task RecordFailure(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        string lastError,
        CancellationToken cancellationToken
    )
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new RetryFailed(
                id,
                options.ApplicationName,
                subscriptionName,
                streamName,
                streamPosition,
                0,
                ExceptionData.FromJson(lastError),
                DateTimeOffset.UtcNow
            ),
            cancellationToken
        );
    }

    public Task Retry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        CancellationToken cancellationToken
    ) => AttemptRetry(id, subscriptionName, streamName, streamPosition, attempts, false, cancellationToken);

    public async Task ManualRetry(Guid id, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(RetryStreamName.For(id), cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        await AttemptRetry(
            id,
            state.SubscriptionName,
            state.StreamName,
            state.StreamPosition,
            state.Attempts,
            true,
            cancellationToken
        );
    }


    private async Task AttemptRetry(
        Guid id,
        string subscriptionName,
        string streamName,
        long streamPosition,
        int attempts,
        bool manualRetry,
        CancellationToken cancellationToken
    )
    {
        var subscription = subscriptionRegistry.GetSubscription(subscriptionName);

        if (subscription == null)
        {
            return;
        }

        var retryStreamName = RetryStreamName.For(id);

        var attemptNumber = attempts + 1;

        try
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            var checkpoint = await database.Execute(
                new LockCheckpoint(options.ApplicationName, subscription.Name, streamName),
                connection,
                transaction,
                cancellationToken
            );

            if (checkpoint == null || checkpoint.Status is CheckpointStatus.Active)
            {
                return;
            }

            await subscriptionStreamProcessor.Process(
                connection,
                transaction,
                subscription,
                streamName,
                checkpoint.StreamPosition,
                1,
                true,
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);

            await messageStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.StreamExists,
                new RetrySucceeded(
                    id,
                    options.ApplicationName,
                    subscriptionName,
                    streamName,
                    streamPosition,
                    attemptNumber,
                    DateTimeOffset.UtcNow
                ),
                cancellationToken
            );
        }
        catch (Exception e)
        {
            if (manualRetry)
            {
                await messageStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new ManualRetryFailed(
                        id,
                        options.ApplicationName,
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        ExceptionData.From(e),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );

                return;
            }

            var maxRetries = subscription.GetMaxRetryCount(e.GetType());

            if (attemptNumber >= maxRetries)
            {
                await messageStore.AppendToStream(
                    retryStreamName,
                    ExpectedVersion.StreamExists,
                    new RetryFailed(
                        id,
                        options.ApplicationName,
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        ExceptionData.From(e),
                        DateTimeOffset.UtcNow
                    ),
                    cancellationToken
                );
            }
            else
            {
                var delay = attemptNumber.GetNextDelayWithExponentialBackoff();

                await messageScheduler.ScheduleMessage(
                    retryStreamName,
                    new RetryAttempted(
                        id,
                        options.ApplicationName,
                        subscriptionName,
                        streamName,
                        streamPosition,
                        attemptNumber,
                        ExceptionData.From(e),
                        DateTimeOffset.UtcNow
                    ),
                    delay,
                    cancellationToken
                );
            }
        }
    }

    private class RetryState : IApply
    {
        public string SubscriptionName { get; private set; } = null!;
        public string StreamName { get; private set; } = null!;
        public long StreamPosition { get; private set; }
        public int Attempts { get; private set; }

        public void Apply(object message)
        {
            switch (message)
            {
                case RetryStarted e:
                    Apply(e);
                    break;
                case RetryAttempted e:
                    Apply(e);
                    break;
                case RetryFailed e:
                    Apply(e);
                    break;
            }
        }

        private void Apply(RetryStarted e)
        {
            SubscriptionName = e.SubscriptionName;
            StreamName = e.StreamName;
            StreamPosition = e.StreamPosition;
            Attempts = 0;
        }

        private void Apply(RetryAttempted e)
        {
            SubscriptionName = e.SubscriptionName;
            StreamName = e.StreamName;
            StreamPosition = e.StreamPosition;
            Attempts = e.Attempts;
        }

        private void Apply(RetryFailed e)
        {
            SubscriptionName = e.SubscriptionName;
            StreamName = e.StreamName;
            StreamPosition = e.StreamPosition;
            Attempts = e.Attempts;
        }
    }
}
