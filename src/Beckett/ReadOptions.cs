namespace Beckett;

public class ReadOptions
{
    public static readonly ReadOptions Default = new();

    public long? StartingStreamPosition { get; set; }
    public long? EndingStreamPosition { get; set; }
    public long? StartingGlobalPosition { get; set; }
    public long? EndingGlobalPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }

    public static ReadOptions Last() => new()
    {
        Count = 1,
        ReadForwards = false
    };
}
