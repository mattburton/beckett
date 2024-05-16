using Beckett.Subscriptions.Retries.Events.Models;

namespace Beckett.Subscriptions.Retries.Events;

public record SubscriptionError(
    string SubscriptionName,
    string StreamName,
    long StreamPosition,
    ExceptionData Exception,
    DateTimeOffset Timestamp
);
