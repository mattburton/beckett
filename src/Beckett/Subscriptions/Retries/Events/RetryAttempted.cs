using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record RetryAttempted(
    Guid Id,
    string SubscriptionGroupName,
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    ExceptionData Exception,
    DateTimeOffset Timestamp
);
