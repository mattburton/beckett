using System.Text.Json;
using UUIDNext;

namespace Beckett.Database.Types;

public class MessageType
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required JsonDocument Data { get; init; }
    public required JsonDocument Metadata { get; init; }
    public long? ExpectedVersion { get; init; }

    public static MessageType From(
        string streamName,
        Message message
    )
    {
        return new MessageType
        {
            Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
            StreamName = streamName,
            Type = message.Type,
            Data = message.Data,
            Metadata = message.SerializedMetadata
        };
    }
}
