using System.Text.Json;

namespace Beckett.Messages;

public static class JsonDocumentExtensions
{
    public static Dictionary<string, object>? ToMetadataDictionary(this JsonDocument metadata)
    {
        try
        {
            return metadata.Deserialize<Dictionary<string, object>>();
        }
        catch
        {
            return null;
        }
    }
}
