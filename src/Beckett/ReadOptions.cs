namespace Beckett;

public class ReadOptions
{
    public static readonly ReadOptions Default = new();

    /// <summary>
    /// The starting position in the stream to read from (inclusive)
    /// </summary>
    public long? StartingStreamPosition { get; set; }

    /// <summary>
    /// The ending position in the stream to read until (inclusive)
    /// </summary>
    public long? EndingStreamPosition { get; set; }

    /// <summary>
    /// The starting global position in the stream to read from (inclusive)
    /// </summary>
    public long? StartingGlobalPosition { get; set; }

    /// <summary>
    /// The ending global position in the stream to read until (inclusive)
    /// </summary>
    public long? EndingGlobalPosition { get; set; }

    /// <summary>
    /// The number of messages to read from the stream
    /// </summary>
    public long? Count { get; set; }

    /// <summary>
    /// Whether to read the stream forwards or backwards in terms of the stream position order
    /// </summary>
    public bool? ReadForwards { get; set; }

    /// <summary>
    /// Require a primary / leader connection when performing the read
    /// </summary>
    public bool? RequirePrimary { get; set; }

    /// <summary>
    /// Read only the last message appended to a given stream. This is a convenience method for setting the
    /// <see cref="Count"/> to 1 and <see cref="ReadForwards"/> to false. Useful for streams used for lookup data or
    /// snapshots of aggregated state to retrieve the latest entry quickly without reading the entire stream.
    /// </summary>
    /// <returns></returns>
    public static ReadOptions Last() => new()
    {
        Count = 1,
        ReadForwards = false
    };
}
