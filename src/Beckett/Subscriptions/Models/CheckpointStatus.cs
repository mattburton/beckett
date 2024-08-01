namespace Beckett.Subscriptions.Models;

public enum CheckpointStatus
{
    Active,
    Lagging,
    Reserved,
    RetryPending,
    Retrying,
    FailurePending,
    Failed,
    Deleted
}
