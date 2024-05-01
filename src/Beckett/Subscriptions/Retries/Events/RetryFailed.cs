using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record RetryFailed(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    int Attempts,
    ExceptionData Exception,
    DateTimeOffset Timestamp
);
