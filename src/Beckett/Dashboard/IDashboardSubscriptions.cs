using Beckett.Database.Types;
using Beckett.Subscriptions;

namespace Beckett.Dashboard;

public interface IDashboardSubscriptions
{
    Task<GetSubscriptionsResult> GetSubscriptions(string? query, int page, int pageSize, CancellationToken cancellationToken);

    Task<GetSubscriptionResult?> GetSubscription(int id, CancellationToken cancellationToken);

    Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetReservationsResult> GetReservations(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetRetriesResult> GetRetries(string? query, int page, int pageSize, CancellationToken cancellationToken);

    Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken);

    Task<GetFailedResult> GetFailed(string? query, int page, int pageSize, CancellationToken cancellationToken);

    Task ReleaseCheckpointReservation(long id, CancellationToken cancellationToken);

    Task ResetSubscription(int id, CancellationToken cancellationToken);
}

public record GetSubscriptionsResult(List<GetSubscriptionsResult.Subscription> Subscriptions, int TotalResults)
{
    public record Subscription(int Id, string GroupName, string SubscriptionName, SubscriptionStatus Status);
}

public record GetSubscriptionResult(int Id, string GroupName, string SubscriptionName, SubscriptionStatus Status);

public record GetLaggingSubscriptionsResult(
    List<GetLaggingSubscriptionsResult.Subscription> Subscriptions,
    int TotalResults
)
{
    public record Subscription(string GroupName, string SubscriptionName, int TotalLag);
}

public record GetReservationsResult(List<GetReservationsResult.Reservation> Reservations, int TotalResults)
{
    public record Reservation(
        long Id,
        string GroupName,
        string SubscriptionName,
        string StreamName,
        long StreamPosition,
        DateTimeOffset ReservedUntil
    );
}

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries, int TotalResults)
{
    public record Retry(
        long Id,
        string GroupName,
        string SubscriptionName,
        string StreamName,
        long StreamPosition,
        DateTimeOffset LastAttempted
    );
}

public class GetCheckpointResult
{
    public required long Id { get; init; }
    public required int SubscriptionId { get; init; }
    public required string GroupName { get; init; }
    public required string SubscriptionName { get; init; }
    public required string StreamName { get; init; }
    public required long StreamVersion { get; init; }
    public required long StreamPosition { get; init; }
    public required CheckpointStatus Status { get; init; }
    public DateTimeOffset? ProcessAt { get; init; }
    public DateTimeOffset? ReservedUntil { get; init; }
    public required RetryType[] Retries { get; init; }

    public int TotalAttempts => Retries?.Length > 0 ? Retries.Length - 1 : 0;

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
        CheckpointStatus.Active => false,
        _ => true
    };
}

public record GetFailedResult(List<GetFailedResult.Failure> Failures, int TotalResults)
{
    public record Failure(
        long Id,
        string GroupName,
        string SubscriptionName,
        string StreamName,
        long StreamPosition,
        DateTimeOffset LastAttempted
    );
}
