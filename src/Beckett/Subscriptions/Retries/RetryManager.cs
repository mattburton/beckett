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
        string lastError,
        CancellationToken cancellationToken
    )
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new RetryStarted(
                id,
                options.Subscriptions.GroupName,
                subscriptionName,
                streamName,
                streamPosition,
                ExceptionData.FromJson(lastError),
                DateTimeOffset.UtcNow
            ),
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
                options.Subscriptions.GroupName,
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
    ) => AttemptRetry(
        id,
        subscriptionName,
        streamName,
        streamPosition,
        attempts,
        false,
        cancellationToken
    );

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

    public async Task DeleteRetry(Guid id, CancellationToken cancellationToken)
    {
        var streamName = RetryStreamName.For(id);

        var stream = await messageStore.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<RetryState>();

        if (state.Status == CheckpointStatus.Deleted)
        {
            return;
        }

        await using var connection = database.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await database.Execute(
            new UpdateCheckpointStatus(
                options.Subscriptions.GroupName,
                state.SubscriptionName,
                state.StreamName,
                state.StreamPosition,
                CheckpointStatus.Deleted,
                retryId: id
            ),
            connection,
            transaction,
            cancellationToken
        );

        await messageStore.AppendToStream(
            streamName,
            ExpectedVersion.StreamExists,
            new RetryDeleted(id, DateTimeOffset.UtcNow),
            cancellationToken
        );

        await transaction.CommitAsync(cancellationToken);
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
                new LockCheckpoint(options.Subscriptions.GroupName, subscription.Name, streamName),
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

            await messageStore.AppendToStream(
                retryStreamName,
                ExpectedVersion.StreamExists,
                new RetrySucceeded(
                    id,
                    options.Subscriptions.GroupName,
                    subscriptionName,
                    streamName,
                    streamPosition,
                    attemptNumber,
                    DateTimeOffset.UtcNow
                ),
                cancellationToken
            );

            await transaction.CommitAsync(cancellationToken);
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
                        options.Subscriptions.GroupName,
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
                        options.Subscriptions.GroupName,
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
                var delay = attemptNumber.GetNextDelayWithExponentialBackoff(
                    options.Subscriptions.Retries.Delay,
                    options.Subscriptions.Retries.MaxDelay
                );

                await messageScheduler.ScheduleMessage(
                    retryStreamName,
                    new RetryAttempted(
                        id,
                        options.Subscriptions.GroupName,
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
        public CheckpointStatus Status { get; private set; }

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
                case RetrySucceeded e:
                    Apply(e);
                    break;
                case RetryFailed e:
                    Apply(e);
                    break;
                case RetryDeleted e:
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
            Status = CheckpointStatus.Retry;
        }

        private void Apply(RetryAttempted e)
        {
            Attempts = e.Attempts;
            Status = CheckpointStatus.Retry;
        }

        private void Apply(RetrySucceeded e)
        {
            Attempts = e.Attempts;
            Status = CheckpointStatus.Active;
        }

        private void Apply(RetryFailed e)
        {
            SubscriptionName = e.SubscriptionName;
            StreamName = e.StreamName;
            StreamPosition = e.StreamPosition;
            Attempts = e.Attempts;
            Status = CheckpointStatus.Failed;
        }

        private void Apply(RetryDeleted _)
        {
            Status = CheckpointStatus.Deleted;
        }
    }
}
