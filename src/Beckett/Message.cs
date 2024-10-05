using System.Text.Json;

namespace Beckett;

public class Message
{
    public required string Type { get; init; }
    public required JsonDocument Data { get; init; }
    public required Dictionary<string, object> Metadata { get; init; } = [];
}
