using Beckett.Subscriptions.Retries;
using Beckett.Subscriptions.Retries.Events;

namespace Beckett.Dashboard.Subscriptions;

public class GetRetryDetailsResult : IApply
{
    public Guid Id { get; private set; }
    public string GroupName { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string StreamName { get; private set; } = null!;
    public long StreamPosition { get; private set; }
    public RetryStatus Status { get; private set; }
    public ExceptionData? Error { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? RetryAt { get; private set; }
    public int TotalAttempts { get; private set; }
    public List<Attempt> Attempts { get; } = [];

    public string StreamCategory
    {
        get
        {
            var firstHyphen = StreamName.IndexOf('-');

            return StreamName[..firstHyphen];
        }
    }

    public bool ShowControls => Status switch
    {
        RetryStatus.Succeeded => false,
        RetryStatus.ManualRetryRequested => false,
        RetryStatus.Deleted => false,
        _ => true
    };

    public void Apply(object message)
    {
        switch (message)
        {
            case RetryStarted e:
                Apply(e);
                break;
            case RetryAttemptFailed e:
                Apply(e);
                break;
            case RetrySucceeded e:
                Apply(e);
                break;
            case RetryFailed e:
                Apply(e);
                break;
            case ManualRetryRequested e:
                Apply(e);
                break;
            case ManualRetryFailed e:
                Apply(e);
                break;
            case DeleteRetryRequested e:
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
        Status = RetryStatus.Started;
        Error = e.Error;
        StartedAt = e.Timestamp;
        RetryAt = e.RetryAt;
    }

    private void Apply(RetryAttemptFailed e)
    {
        Status = RetryStatus.Scheduled;
        RetryAt = e.RetryAt;
        TotalAttempts++;

        Attempts.Add(new Attempt(Status, e.Timestamp, e.Error));
    }

    private void Apply(RetrySucceeded e)
    {
        Status = RetryStatus.Succeeded;
        TotalAttempts++;

        Attempts.Add(new Attempt(Status, e.Timestamp, null));
    }

    private void Apply(RetryFailed e)
    {
        Id = e.Id;
        Status = RetryStatus.Failed;
        RetryAt = null;

        Attempts.Add(new Attempt(Status, e.Timestamp, null));
    }

    private void Apply(ManualRetryRequested e)
    {
        Status = RetryStatus.ManualRetryRequested;

        Attempts.Add(new Attempt(Status, e.Timestamp, null));
    }

    private void Apply(ManualRetryFailed e)
    {
        Status = RetryStatus.ManualRetryFailed;
        TotalAttempts++;

        Attempts.Add(new Attempt(Status, e.Timestamp, e.Error));
    }

    private void Apply(DeleteRetryRequested _)
    {
        Status = RetryStatus.DeleteRequested;
    }

    private void Apply(RetryDeleted e)
    {
        Status = RetryStatus.Deleted;
        RetryAt = null;

        Attempts.Add(new Attempt(Status, e.Timestamp, null));
    }

    public record Attempt(RetryStatus Status, DateTimeOffset Timestamp, ExceptionData? Error)
    {
        public string BackgroundColor => Status switch
        {
            RetryStatus.Succeeded => "text-bg-success",
            RetryStatus.ManualRetryRequested => "text-bg-secondary",
            _ => "text-bg-danger"
        };
    }

    public enum RetryStatus
    {
        Started,
        Scheduled,
        Succeeded,
        Failed,
        ManualRetryRequested,
        ManualRetryFailed,
        DeleteRequested,
        Deleted
    }
}

public static class RetryStatusExtensions
{
    public static string ToDisplayStatus(this GetRetryDetailsResult.RetryStatus status)
    {
        return status switch
        {
            GetRetryDetailsResult.RetryStatus.Started => "Pending Retry",
            GetRetryDetailsResult.RetryStatus.Scheduled => "Retrying",
            GetRetryDetailsResult.RetryStatus.Succeeded => "Succeeded",
            GetRetryDetailsResult.RetryStatus.Failed => "Failed",
            GetRetryDetailsResult.RetryStatus.ManualRetryRequested => "Manual Retry Requested",
            GetRetryDetailsResult.RetryStatus.ManualRetryFailed => "Manual Retry Failed",
            GetRetryDetailsResult.RetryStatus.DeleteRequested => "Delete Requested",
            GetRetryDetailsResult.RetryStatus.Deleted => "Deleted",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static string ToAttemptStatus(this GetRetryDetailsResult.RetryStatus status)
    {
        return status switch
        {
            GetRetryDetailsResult.RetryStatus.Scheduled => "Retried",
            GetRetryDetailsResult.RetryStatus.Succeeded => "Succeeded",
            _ => ToDisplayStatus(status)
        };
    }
}
