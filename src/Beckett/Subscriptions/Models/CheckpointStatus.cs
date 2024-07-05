namespace Beckett.Subscriptions.Models;

public enum CheckpointStatus
{
    Active,
    Retry,
    PendingFailure,
    Failed
}
