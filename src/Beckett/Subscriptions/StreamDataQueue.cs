using System.Threading.Channels;

namespace Beckett.Subscriptions;

public static class StreamDataQueue
{
    private static readonly Channel<StreamData> _channel = Channel.CreateUnbounded<StreamData>();

    public static ChannelReader<StreamData> Reader => _channel.Reader;

    public static void Enqueue(string[] categories, DateTimeOffset[] categoryTimestamps, string[] tenants)
    {
        _channel.Writer.TryWrite(new StreamData(categories, categoryTimestamps, tenants));
    }

    public record StreamData(string[] Categories, DateTimeOffset[] CategoryTimestamps, string[] Tenants);
}
