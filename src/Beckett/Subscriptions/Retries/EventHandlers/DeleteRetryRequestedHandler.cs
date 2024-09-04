using Beckett.Database;
using Beckett.Subscriptions.Queries;
using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries.EventHandlers;

public class DeleteRetryRequestedHandler(IMessageStore messageStore, IPostgresDatabase database)
{
    public async Task Handle(DeleteRetryRequested message, CancellationToken cancellationToken)
    {
        var streamName = RetryStreamName.For(message.Id);

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
                state.GroupName,
                state.Name,
                state.StreamName,
                state.StreamPosition,
                CheckpointStatus.Deleted
            ),
            connection,
            transaction,
            cancellationToken
        );

        await messageStore.AppendToStream(
            streamName,
            ExpectedVersion.StreamExists,
            new RetryDeleted(message.Id, DateTimeOffset.UtcNow),
            cancellationToken
        );

        await transaction.CommitAsync(cancellationToken);
    }
}
