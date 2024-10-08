using System.Text.Json;

namespace Beckett.Messages;

public static class JsonDocumentExtensions
{
    public static Dictionary<string, object> ToMetadataDictionary(this JsonDocument metadata)
    {
        return metadata.Deserialize<Dictionary<string, object>>() ?? new Dictionary<string, object>();
    }
}
