using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions;

public class GetRetryDetailsResult
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = null!;
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

    public record Attempt(
        RetryStatus Status,
        bool Success,
        DateTimeOffset Timestamp,
        ExceptionData? Exception
    );
}

public static class RetryStatusExtensions
{
    public static string ToDisplayStatus(this RetryStatus status)
    {
        return status switch
        {
            RetryStatus.Started => "Pending Retry",
            RetryStatus.Scheduled => "Retrying",
            RetryStatus.Succeeded => "Succeeded",
            RetryStatus.Failed => "Failed",
            RetryStatus.ManualRetryRequested => "Manual Retry Requested",
            RetryStatus.ManualRetryFailed => "Manual Retry Failed",
            RetryStatus.Deleted => "Deleted",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }

    public static string ToAttemptStatus(this RetryStatus status)
    {
        return status switch
        {
            RetryStatus.Scheduled => "Retried",
            RetryStatus.Succeeded => "Succeeded",
            _ => ToDisplayStatus(status)
        };
    }
}
