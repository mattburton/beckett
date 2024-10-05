using System.Text.Json;
using Beckett.Messages;

namespace Beckett.Database.Types;

public class ScheduledMessageType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public JsonDocument Data { get; init; } = null!;
    public JsonDocument Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static ScheduledMessageType From(
        Guid id,
        object message,
        Dictionary<string, object> metadata,
        DateTimeOffset deliverAt
    )
    {
        string type;

        if (message is Message genericMessage)
        {
            type = genericMessage.Type;
        }
        else
        {
            type = MessageTypeMap.GetName(message.GetType());
        }

        return new ScheduledMessageType
        {
            Id = id,
            Type = type,
            Data = StaticMessageSerializer.Serialize(message),
            Metadata = metadata.ToJsonDocument(),
            DeliverAt = deliverAt
        };
    }
}
