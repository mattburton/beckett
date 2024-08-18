namespace Beckett.MessageStorage;

public class ReadStreamOptions
{
    public long? StartingStreamPosition { get; set; }
    public long? EndingStreamPosition { get; set; }
    public long? StartingGlobalPosition { get; set; }
    public long? EndingGlobalPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }

    public static ReadStreamOptions From(ReadOptions options)
    {
        return new ReadStreamOptions
        {
            StartingStreamPosition = options.StartingStreamPosition,
            EndingStreamPosition = options.EndingStreamPosition,
            StartingGlobalPosition = options.StartingGlobalPosition,
            EndingGlobalPosition = options.EndingGlobalPosition,
            Count = options.Count,
            ReadForwards = options.ReadForwards
        };
    }
}
