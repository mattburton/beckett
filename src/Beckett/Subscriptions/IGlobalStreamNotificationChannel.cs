using System.Threading.Channels;

namespace Beckett.Subscriptions;

public interface IGlobalStreamNotificationChannel
{
    void Notify();

    Channel<MessagesAvailable> For(string groupName);
    
    Channel<MessagesAvailable> Global { get; }
}

public class GlobalStreamNotificationChannel(BeckettOptions options) : IGlobalStreamNotificationChannel
{
    private readonly Dictionary<string, Channel<MessagesAvailable>> _channels = BuildChannels(options);
    private readonly Channel<MessagesAvailable> _globalChannel = Channel.CreateBounded<MessagesAvailable>(100);

    public Channel<MessagesAvailable> Global => _globalChannel;

    public void Notify()
    {
        // Notify all per-group channels (for backward compatibility)
        foreach (var channel in _channels.Values)
        {
            channel.Writer.TryWrite(MessagesAvailable.Instance);
        }

        // Notify the global channel
        _globalChannel.Writer.TryWrite(MessagesAvailable.Instance);
    }

    public Channel<MessagesAvailable> For(string groupName) => _channels[groupName];

    private static Dictionary<string, Channel<MessagesAvailable>> BuildChannels(BeckettOptions options)
    {
        return options.Subscriptions.Groups.ToDictionary(
            group => group.Name,
            group => Channel.CreateBounded<MessagesAvailable>(group.GetConcurrency() * 2)
        );
    }
}
