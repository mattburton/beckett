namespace Beckett.Subscriptions.Retries;

public enum RetryStatus
{
    Started,
    Reserved,
    Scheduled,
    Succeeded,
    Failed,
    ManualRetryRequested,
    ManualRetryFailed,
    Deleted
}
