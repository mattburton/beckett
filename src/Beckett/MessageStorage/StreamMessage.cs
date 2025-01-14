using System.Text.Json;

namespace Beckett.MessageStorage;

public record StreamMessage(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonDocument Data,
    JsonDocument Metadata,
    DateTimeOffset Timestamp
);
