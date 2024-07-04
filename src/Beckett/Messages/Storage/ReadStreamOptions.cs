using Npgsql;

namespace Beckett.Messages.Storage;

public class ReadStreamOptions
{
    public static readonly ReadOptions Default = new();

    public long? StartingStreamPosition { get; set; }
    public long? Count { get; set; }
    public bool? ReadForwards { get; set; }
    public NpgsqlConnection? Connection { get; set; }

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
