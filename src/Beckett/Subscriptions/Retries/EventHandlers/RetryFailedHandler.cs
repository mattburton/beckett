using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Subscriptions.Models;
using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class RetryFailedHandler(IPostgresDatabase database, BeckettOptions options)
{
    public async Task Handle(RetryFailed message, CancellationToken cancellationToken) =>
        await database.Execute(
            new UpdateCheckpointStatus(
                options.ApplicationName,
                message.SubscriptionName,
                message.StreamName,
                message.StreamPosition,
                CheckpointStatus.Failed
            ),
            cancellationToken
        );
}
