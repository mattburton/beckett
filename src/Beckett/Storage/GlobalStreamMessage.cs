namespace Beckett.Storage;

public record GlobalStreamMessage(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string MessageType,
    string? Tenant,
    string? CorrelationId,
    byte[] Metadata,
    DateTimeOffset Timestamp
);
