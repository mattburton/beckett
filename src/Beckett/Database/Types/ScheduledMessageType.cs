using Beckett.Messages;

namespace Beckett.Database.Types;

public class ScheduledMessageType
{
    public string Application { get; init; } = null!;
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static ScheduledMessageType From(
        string application,
        Guid id,
        object message,
        Dictionary<string, object> metadata,
        DateTimeOffset deliverAt,
        IMessageSerializer messageSerializer
    )
    {
        var result = messageSerializer.Serialize(message, metadata);

        return new ScheduledMessageType
        {
            Application = application,
            Id = id,
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata,
            DeliverAt = deliverAt
        };
    }
}
