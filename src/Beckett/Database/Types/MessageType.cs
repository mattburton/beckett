using Beckett.Messages;

namespace Beckett.Database.Types;

public class MessageType
{
    public Guid Id { get; init; }
    public string? StreamName { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public long? ExpectedVersion { get; init; }

    public static MessageType From(
        object message,
        Dictionary<string, object> metadata,
        IMessageSerializer messageSerializer
    )
    {
        var result = messageSerializer.Serialize(message, metadata);

        return new MessageType
        {
            Id = Guid.NewGuid(),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}
