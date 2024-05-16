using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class SubscriptionRetryFailedHandler(IPostgresDatabase database, SubscriptionOptions options)
{
    public async Task Handle(SubscriptionRetryFailed message, CancellationToken cancellationToken)
    {
        await database.Execute(
            new UpdateCheckpointStatus(
                options.ApplicationName,
                message.SubscriptionName,
                message.Topic,
                message.StreamId,
                message.StreamPosition,
                CheckpointStatus.Failed
            ),
            cancellationToken
        );
    }
}
