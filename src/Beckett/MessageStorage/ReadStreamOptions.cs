namespace Beckett.MessageStorage;

public class ReadStreamOptions
{
    public long? StartingStreamPosition { get; init; }
    public long? EndingStreamPosition { get; init; }
    public long? StartingGlobalPosition { get; init; }
    public long? EndingGlobalPosition { get; init; }
    public int? Count { get; init; }
    public bool? ReadForwards { get; init; }
    public bool? RequirePrimary { get; init; }

    public static ReadStreamOptions From(ReadOptions options)
    {
        return new ReadStreamOptions
        {
            StartingStreamPosition = options.StartingStreamPosition,
            EndingStreamPosition = options.EndingStreamPosition,
            StartingGlobalPosition = options.StartingGlobalPosition,
            EndingGlobalPosition = options.EndingGlobalPosition,
            Count = options.Count,
            ReadForwards = options.ReadForwards,
            RequirePrimary = options.RequirePrimary
        };
    }
}
