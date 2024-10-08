namespace Beckett.Subscriptions;

public interface ICheckpointProcessor
{
    Task Process(
        Checkpoint checkpoint,
        Subscription subscription,
        CancellationToken cancellationToken
    );
}
