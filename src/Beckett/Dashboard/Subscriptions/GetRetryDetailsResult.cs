using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions;

public class GetRetryDetailsResult
{
    public required Guid Id { get; init; }
    public required string GroupName { get; init; }
    public required string SubscriptionName { get; init; }
    public required string StreamName { get; init; }
    public required long StreamPosition { get; init; }
    public required RetryStatus Status { get; init; }
    public ExceptionData? Exception { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset? RetryAt { get; init; }
    public required int TotalAttempts { get; init; }
    public required List<Attempt> Attempts { get; init; } = [];

    public string StreamCategory
    {
        get
        {
            var firstHyphen = StreamName.IndexOf('-');

            return StreamName[..firstHyphen];
        }
    }

    public bool ShowControls => Status is RetryStatus.Failed or RetryStatus.ManualRetryFailed;

    public record Attempt(
        RetryStatus Status,
        DateTimeOffset Timestamp,
        ExceptionData? Exception
    )
    {
        public string BackgroundColor => Status switch
        {
            RetryStatus.Succeeded => "text-bg-success",
            RetryStatus.ManualRetryRequested => "text-bg-secondary",
            _ => "text-bg-danger"
        };
    }
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
