using System.Text.Json;

namespace Beckett.Database.Types;

public class RetryType
{
    public int Attempt { get; init; }
    public JsonDocument Error { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }
}
