namespace Beckett.Subscriptions.Models;

public record Checkpoint(
    string Application,
    string Name,
    string StreamName,
    long StreamPosition,
    long StreamVersion,
    bool Blocked
);
