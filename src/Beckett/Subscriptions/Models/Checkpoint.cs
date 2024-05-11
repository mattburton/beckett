namespace Beckett.Subscriptions.Models;

public record Checkpoint(
    string Application,
    string Name,
    string Topic,
    string StreamId,
    long StreamPosition,
    long StreamVersion,
    bool Blocked
);
