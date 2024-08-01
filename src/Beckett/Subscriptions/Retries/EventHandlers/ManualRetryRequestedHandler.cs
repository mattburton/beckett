using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class ManualRetryRequestedHandler(IRetryManager retryManager)
{
    public Task Handle(ManualRetryRequested e, CancellationToken cancellationToken) =>
        retryManager.ManualRetry(e.CheckpointId, cancellationToken);
}
