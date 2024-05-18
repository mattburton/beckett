using Beckett.Messages;

namespace Beckett;

public static class MetadataExtensions
{
    public static object WithMetadata(this object message, Dictionary<string, object> metadata) =>
        new MessageMetadataWrapper(message, metadata);
}
