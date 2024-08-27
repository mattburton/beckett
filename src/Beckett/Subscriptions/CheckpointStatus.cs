namespace Beckett.Subscriptions;

public enum CheckpointStatus
{
    Active,
    Lagging,
    Reserved,
    PendingRetry,
    Retry,
    Failed,
    Deleted
}
