using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class BulkDeleteRequestedHandler(IMessageStore messageStore)
{
    public async Task Handle(BulkDeleteRequested message, CancellationToken cancellationToken)
    {
        foreach (var retryId in message.RetryIds)
        {
            await messageStore.AppendToStream(
                RetryStreamName.For(retryId),
                ExpectedVersion.Any,
                new DeleteRetryRequested(retryId, message.Timestamp),
                cancellationToken
            );
        }
    }
}
