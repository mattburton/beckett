using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Subscriptions.Retries;

public class RetryState : IApply
{
    public Guid Id { get; private set; }
    public string GroupName { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string StreamName { get; private set; } = null!;
    public long StreamPosition { get; private set; }
    public int Attempts { get; private set; }
    public int MaxRetryCount { get; private set; }
    public CheckpointStatus Status { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public bool Failed { get; private set; }

    public bool Completed => Status is CheckpointStatus.Active or CheckpointStatus.Failed or CheckpointStatus.Deleted;

    public void Apply(object message)
    {
        switch (message)
        {
            case RetryStarted e:
                Apply(e);
                break;
            case RetryScheduled e:
                Apply(e);
                break;
            case RetryAttemptFailed e:
                Apply(e);
                break;
            case ManualRetryFailed e:
                Apply(e);
                break;
            case RetrySucceeded e:
                Apply(e);
                break;
            case RetryFailed e:
                Apply(e);
                break;
            case RetryDeleted e:
                Apply(e);
                break;
        }
    }

    private void Apply(RetryStarted e)
    {
        Id = e.Id;
        GroupName = e.GroupName;
        Name = e.Name;
        StreamName = e.StreamName;
        StreamPosition = e.StreamPosition;
        MaxRetryCount = e.MaxRetryCount;
        Status = CheckpointStatus.Retry;
    }

    private void Apply(RetryScheduled e)
    {
        NextRetryAt ??= e.RetryAt;
    }

    private void Apply(RetryAttemptFailed e)
    {
        Status = CheckpointStatus.Retry;
        NextRetryAt = null;
        Attempts = e.Attempt;
    }

    private void Apply(ManualRetryFailed e)
    {
        Status = CheckpointStatus.Retry;
        Attempts = e.Attempt;
    }

    private void Apply(RetrySucceeded e)
    {
        Status = CheckpointStatus.Active;
        NextRetryAt = null;
        Attempts = e.Attempt;
    }

    private void Apply(RetryFailed e)
    {
        Status = CheckpointStatus.Failed;
        NextRetryAt = null;
        Failed = true;
        Attempts = e.Attempt;
    }

    private void Apply(RetryDeleted _)
    {
        Status = CheckpointStatus.Deleted;
        NextRetryAt = null;
    }
}
