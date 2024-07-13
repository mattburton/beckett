namespace Beckett.Subscriptions.Models;

public record Checkpoint(
    string GroupName,
    string Name,
    string StreamName,
    long StreamPosition,
    long StreamVersion,
    CheckpointStatus Status
);
