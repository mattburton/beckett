using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class DeleteRetryRequestedHandler(IRetryManager retryManager)
{
    public Task Handle(DeleteRetryRequested e, CancellationToken cancellationToken) =>
        retryManager.DeleteRetry(e.CheckpointId, cancellationToken);
}
