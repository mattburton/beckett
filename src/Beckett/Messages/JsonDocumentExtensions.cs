using System.Text.Json;

namespace Beckett.Messages;

public static class JsonDocumentExtensions
{
    public static Dictionary<string, string> ToMetadataDictionary(this JsonDocument metadata)
    {
        return metadata.Deserialize<Dictionary<string, string>>(MessageSerializer.Options) ??
               new Dictionary<string, string>();
    }
}
