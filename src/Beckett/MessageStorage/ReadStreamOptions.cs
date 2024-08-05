namespace Beckett.MessageStorage;

public class ReadStreamOptions
{
    public long? StartingStreamPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }

    public static ReadStreamOptions From(ReadOptions options)
    {
        return new ReadStreamOptions
        {
            StartingStreamPosition = options.StartingStreamPosition,
            Count = options.Count,
            ReadForwards = options.ReadForwards
        };
    }
}
