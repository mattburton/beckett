using System.Threading.Channels;

namespace Beckett.Subscriptions.Initialization;

public interface ISubscriptionInitializerChannel
{
    void Notify(SubscriptionGroup group);

    Channel<UninitializedSubscriptionAvailable> For(SubscriptionGroup group);
}

public class SubscriptionInitializerChannel(BeckettOptions options)
    : ISubscriptionInitializerChannel
{
    private readonly Dictionary<string, Channel<UninitializedSubscriptionAvailable>> _channels = BuildChannels(options);

    public void Notify(SubscriptionGroup group)
    {
        if (!_channels.TryGetValue(group.Name, out var channel))
        {
            return;
        }

        for (var i = 0; i < group.InitializationConcurrency; i++)
        {
            channel.Writer.TryWrite(UninitializedSubscriptionAvailable.Instance);
        }
    }

    public Channel<UninitializedSubscriptionAvailable> For(SubscriptionGroup group) => _channels[group.Name];

    private static Dictionary<string, Channel<UninitializedSubscriptionAvailable>> BuildChannels(BeckettOptions options)
    {
        return options.Subscriptions.Groups.ToDictionary(
            group => group.Name,
            group => Channel.CreateBounded<UninitializedSubscriptionAvailable>(group.InitializationConcurrency)
        );
    }
}

public readonly struct UninitializedSubscriptionAvailable
{
    public static UninitializedSubscriptionAvailable Instance { get; } = new();
}
