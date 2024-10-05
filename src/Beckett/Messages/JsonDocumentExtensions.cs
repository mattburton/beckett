using System.Text.Json;

namespace Beckett.Messages;

public static class JsonDocumentExtensions
{
    public static Dictionary<string, object> ToDictionary(this JsonDocument document)
    {
        return document.Deserialize<Dictionary<string, object>>() ?? throw new Exception(
            $"Unable to deserialize JSON as dictionary: {document}"
        );
    }

    public static JsonDocument ToJsonDocument(this Dictionary<string, object> dictionary)
    {
        return JsonSerializer.SerializeToDocument(dictionary);
    }
}
