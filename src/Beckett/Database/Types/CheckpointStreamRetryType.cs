using System.Text.Json;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Retries;

namespace Beckett.Database.Types;

public class CheckpointStreamRetryType
{
    public string StreamName { get; init; } = null!;
    public long StreamVersion { get; init; }
    public long StreamPosition { get; init; }
    public JsonElement Error { get; init; }

    public static CheckpointStreamRetryType From(CheckpointStreamError streamError)
    {
        return new CheckpointStreamRetryType
        {
            StreamName = streamError.StreamName,
            StreamVersion = streamError.StreamPosition,
            StreamPosition = streamError.StreamPosition > 0 ? streamError.StreamPosition - 1 : 0,
            Error = ExceptionData.From(streamError.Exception).ToJson().RootElement
        };
    }
}
