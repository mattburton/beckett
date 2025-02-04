using System.Text.Json;
using Beckett.Subscriptions.Retries;

namespace Beckett.Database.Types;

public class RetryType
{
    public int Attempt { get; init; }
    public JsonElement Error { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public ExceptionData GetException() => ExceptionData.FromJson(Error)!;
}
