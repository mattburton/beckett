namespace Beckett.Subscriptions;

public enum CheckpointStatus
{
    Active,
    Lagging,
    Reserved,
    Retry,
    Failed,
    Deleted
}
