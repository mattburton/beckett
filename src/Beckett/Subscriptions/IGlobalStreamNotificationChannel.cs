using System.Threading.Channels;

namespace Beckett.Subscriptions;

public interface IGlobalStreamNotificationChannel
{
    void Notify();

    Channel<MessagesAvailable> For(string groupName);
}

public class GlobalStreamNotificationChannel(BeckettOptions options) : IGlobalStreamNotificationChannel
{
    private readonly Dictionary<string, Channel<MessagesAvailable>> _channels = BuildChannels(options);

    public void Notify()
    {
        foreach (var channel in _channels.Values)
        {
            channel.Writer.TryWrite(MessagesAvailable.Instance);
        }
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
