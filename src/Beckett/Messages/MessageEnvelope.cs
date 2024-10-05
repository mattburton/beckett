using System.Text.Json;

namespace Beckett.Messages;

public readonly struct MessageEnvelope(string type, JsonDocument data, Dictionary<string, object> metadata)
{
    public string Type { get; } = type;
    public JsonDocument Data { get; } = data;
    public Dictionary<string, object> Metadata { get; } = metadata;
}
