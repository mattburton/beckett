using Beckett.Subscriptions;
using Microsoft.Extensions.Hosting;

namespace Beckett.Messages.Services;

public class MessageTypeMappingDiagnosticService(
    BeckettOptions options,
    ISubscriptionRegistry subscriptionRegistry,
    IMessageTypeMap messageTypeMap,
    IHostApplicationLifetime applicationLifetime
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Messages.AllowDynamicTypeMapping)
        {
            return Task.CompletedTask;
        }

        var unmappedMessageTypes = new HashSet<Type>();

        foreach (var subscription in subscriptionRegistry.All())
        {
            foreach (var unmappedMessageType in subscription.MessageTypes.Where(x => !messageTypeMap.IsMapped(x)))
            {
                unmappedMessageTypes.Add(unmappedMessageType);
            }
        }

        if (unmappedMessageTypes.Count == 0)
        {
            return Task.CompletedTask;
        }

        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine("");

        Console.WriteLine(
            $"Beckett: {nameof(MessageOptions.AllowDynamicTypeMapping)} is disabled and the following message types are not mapped:"
        );

        Console.WriteLine("");

        foreach (var messageType in unmappedMessageTypes)
        {
            Console.WriteLine(messageType.FullName);
        }

        Console.WriteLine("");

        Console.ResetColor();

        applicationLifetime.StopApplication();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
