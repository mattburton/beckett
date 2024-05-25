using Beckett.Messages;

namespace Beckett.Database.Types;

public class ScheduledMessageType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static ScheduledMessageType From(
        object message,
        Dictionary<string, object> metadata,
        DateTimeOffset deliverAt,
        IMessageSerializer messageSerializer
    )
    {
        var result = messageSerializer.Serialize(message, metadata);

        return new ScheduledMessageType
        {
            Id = Guid.NewGuid(),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata,
            DeliverAt = deliverAt
        };
    }
}
