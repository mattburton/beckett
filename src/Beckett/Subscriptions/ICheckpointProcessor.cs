namespace Beckett.Subscriptions;

public interface ICheckpointProcessor
{
    Task Process(int instance, Checkpoint checkpoint, Subscription subscription);
}
