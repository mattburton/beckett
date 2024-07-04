namespace Beckett.Dashboard.MessageStore;

public record GetMessageResult(
    string Id,
    string Category,
    string StreamName,
    string Type,
    string Data,
    Dictionary<string, object> Metadata
);
