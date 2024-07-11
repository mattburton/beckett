using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public interface IRetryInformation
{
    Guid Id { get; init; }
    string ApplicationName { get; init; }
    string SubscriptionName { get; init; }
    string StreamName { get; init; }
    long StreamPosition { get; init; }
}

public record RetryStarted(
    Guid Id,
    string ApplicationName,
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    ExceptionData? Exception,
    DateTimeOffset Timestamp
) : IRetryInformation;
