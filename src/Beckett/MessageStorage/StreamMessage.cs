using System.Text.Json;

namespace Beckett.MessageStorage;

public record StreamMessage(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonElement Data,
    JsonElement Metadata,
    DateTimeOffset Timestamp
);
