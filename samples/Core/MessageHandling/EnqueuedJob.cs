using System.Text.Json;

namespace Core.MessageHandling;

public record EnqueuedJob(string Type, JsonElement Data);
