using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class BulkRetryRequestedHandler(IMessageStore messageStore)
{
    public async Task Handle(BulkRetryRequested message, CancellationToken cancellationToken)
    {
        foreach (var retryId in message.RetryIds)
        {
            await messageStore.AppendToStream(
                RetryStreamName.For(retryId),
                ExpectedVersion.Any,
                new ManualRetryRequested(retryId, message.Timestamp),
                cancellationToken
            );
        }
    }
}
