using Beckett.Subscriptions.Retries.Events;
using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Dashboard.Subscriptions;

public class GetRetryDetailsResult : IApply
{
    public Guid Id { get; set; }
    public string ApplicationName { get; set; } = null!;
    public string SubscriptionName { get; set; } = null!;
    public string StreamName { get; set; } = null!;
    public long StreamPosition { get; set; }
    public RetryStatus Status { get; set; }
    public ExceptionData? Exception { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public List<Attempt> Attempts { get; set; } = [];

    public string StreamCategory
    {
        get
        {
            var firstHyphen = StreamName.IndexOf('-');

            return StreamName[..firstHyphen];
        }
    }

    public void Apply(object message)
    {
        switch (message)
        {
            case RetryStarted e:
                Apply(e);
                break;
            case RetryAttempted e:
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
        ApplicationName = e.ApplicationName;
        SubscriptionName = e.SubscriptionName;
        StreamName = e.StreamName;
        StreamPosition = e.StreamPosition;
        Exception = e.Exception;
        StartedAt = e.Timestamp;
        Status = RetryStatus.Started;
    }

    private void Apply(RetryAttempted e)
    {
        Status = RetryStatus.Retrying;

        Attempts.Add(new Attempt(Status, false, e.Timestamp, e.Exception));
    }

    private void Apply(RetrySucceeded e)
    {
        Status = RetryStatus.Success;

        Attempts.Add(new Attempt(Status, true, e.Timestamp, null));
    }

    private void Apply(RetryFailed e)
    {
        Id = e.Id;
        ApplicationName = e.ApplicationName;
        SubscriptionName = e.SubscriptionName;
        StreamName = e.StreamName;
        StreamPosition = e.StreamPosition;
        Exception = e.Exception;
        StartedAt = e.Timestamp;
        Status = RetryStatus.Failed;

        Attempts.Add(new Attempt(Status, false, e.Timestamp, e.Exception));
    }

    private void Apply(ManualRetryRequested _)
    {
        Status = RetryStatus.ManualRetryRequested;
    }

    private void Apply(ManualRetryFailed e)
    {
        Status = RetryStatus.ManualRetryFailed;

        Attempts.Add(new Attempt(Status, false, e.Timestamp, e.Exception));
    }

    private void Apply(DeleteRetryRequested _)
    {
        Status = RetryStatus.DeleteRequested;
    }

    private void Apply(RetryDeleted _)
    {
        Status = RetryStatus.Deleted;
    }

    public record Attempt(
        RetryStatus Status,
        bool Success,
        DateTimeOffset Timestamp,
        ExceptionData? Exception);

    public enum RetryStatus
    {
        Started,
        Retrying,
        Success,
        Failed,
        ManualRetryRequested,
        ManualRetryFailed,
        DeleteRequested,
        Deleted
    }
}

public static class RetryStatusExtensions
{
    public static string ToRetryStatus(this GetRetryDetailsResult.RetryStatus status)
    {
        return status switch
        {
            GetRetryDetailsResult.RetryStatus.Started => "Pending Retry",
            GetRetryDetailsResult.RetryStatus.Retrying => "Retrying",
            GetRetryDetailsResult.RetryStatus.Success => "Success",
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
            GetRetryDetailsResult.RetryStatus.Retrying => "Retried",
            GetRetryDetailsResult.RetryStatus.Success => "Succeeded",
            _ => ToRetryStatus(status)
        };
    }
}
