using System.Threading.Channels;

namespace Beckett.Subscriptions;

public interface ICheckpointNotificationChannel
{
    void Notify(string groupName);

    Channel<CheckpointAvailable> For(string groupName);
}

public class CheckpointNotificationChannel(BeckettOptions options) : ICheckpointNotificationChannel
{
    private readonly Dictionary<string, Channel<CheckpointAvailable>> _channels = BuildChannels(options);

    public void Notify(string groupName)
    {
        if (!_channels.TryGetValue(groupName, out var channel))
        {
            return;
        }

        channel.Writer.TryWrite(CheckpointAvailable.Instance);
    }

    public Channel<CheckpointAvailable> For(string groupName) => _channels[groupName];

    private static Dictionary<string, Channel<CheckpointAvailable>> BuildChannels(BeckettOptions options)
    {
        return options.Subscriptions.Groups.ToDictionary(
            group => group.Name,
            group => Channel.CreateBounded<CheckpointAvailable>(group.GetConcurrency() * 2)
        );
    }
}
