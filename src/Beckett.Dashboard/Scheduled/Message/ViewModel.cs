namespace Beckett.Dashboard.Scheduled.Message;

public record ViewModel(
    Guid Id,
    string StreamName,
    string Type,
    DateTimeOffset DeliverAt,
    DateTimeOffset Timestamp,
    string Data,
    Dictionary<string, string> Metadata
);
