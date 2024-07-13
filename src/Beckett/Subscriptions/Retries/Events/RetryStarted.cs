using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record RetryStarted(
    Guid Id,
    string SubscriptionGroupName,
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    ExceptionData? Exception,
    DateTimeOffset Timestamp
);
