using System.Text.Json;

namespace Beckett.Messages;

public static class EmptyJsonElement
{
    public static readonly JsonElement Instance = JsonDocument.Parse("{}").RootElement.Clone();
}
