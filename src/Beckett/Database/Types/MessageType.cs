using System.Text.Json;
using Beckett.Messages;

namespace Beckett.Database.Types;

public class MessageType
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required JsonElement Data { get; init; }
    public required JsonElement Metadata { get; init; }
    public long? ExpectedVersion { get; init; }

    public static MessageType From(
        string streamName,
        Message message
    )
    {
        return new MessageType
        {
            Id = MessageId.New(),
            StreamName = streamName,
            Type = message.Type,
            Data = message.Data,
            Metadata = message.SerializedMetadata
        };
    }
}
