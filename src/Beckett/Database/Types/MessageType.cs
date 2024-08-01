using Beckett.Messages;
using UUIDNext;

namespace Beckett.Database.Types;

public class MessageType
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required string Data { get; init; }
    public required string Metadata { get; init; }
    public long? ExpectedVersion { get; init; }

    public static MessageType From(
        string streamName,
        object message,
        Dictionary<string, object> metadata,
        IMessageSerializer messageSerializer
    )
    {
        var result = messageSerializer.Serialize(message, metadata);

        return new MessageType
        {
            Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
            StreamName = streamName,
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}
