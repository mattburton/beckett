using System.Text.Json;

namespace Core.Processors;

public record EnqueuedJob(string Type, JsonElement Data);
