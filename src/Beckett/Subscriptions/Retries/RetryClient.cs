using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries;

public class RetryClient(IMessageStore messageStore) : IRetryClient
{
    public async Task ManualRetry(Guid id, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new ManualRetryRequested(id, DateTimeOffset.UtcNow),
            cancellationToken
        );
    }

    public async Task DeleteRetry(Guid id, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            RetryStreamName.For(id),
            ExpectedVersion.Any,
            new DeleteRetryRequested(id, DateTimeOffset.UtcNow),
            cancellationToken
        );
    }
}
