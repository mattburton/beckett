using System.Text.Json;

namespace Core.Jobs;

public record EnqueuedJob(string Type, JsonElement Data);
