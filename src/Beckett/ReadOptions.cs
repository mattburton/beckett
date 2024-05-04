namespace Beckett;

public class ReadOptions
{
    public static readonly ReadOptions Default = new();

    public long? StartingStreamPosition { get; set; }
    public long? EndingGlobalPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }
}
