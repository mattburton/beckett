using System.Text.Json;

namespace Beckett.Database.Types;

public class ScheduledMessageType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public JsonElement Data { get; init; }
    public JsonElement Metadata { get; init; }
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static ScheduledMessageType From(
        Guid id,
        Message message,
        DateTimeOffset deliverAt
    )
    {
        return new ScheduledMessageType
        {
            Id = id,
            Type = message.Type,
            Data = message.Data,
            Metadata = message.SerializedMetadata,
            DeliverAt = deliverAt
        };
    }
}
