using Beckett.Messages;
using Beckett.Scheduling;
using Beckett.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett.Configuration;

public class BeckettBuilder(
    IConfiguration configuration,
    IHostEnvironment environment,
    IServiceCollection services
) : IBeckettBuilder
{
    public IConfiguration Configuration { get; } = configuration;
    public IHostEnvironment Environment { get; } = environment;
    public IServiceCollection Services { get; } = services;

    public ISubscriptionBuilder AddSubscription(string name)
    {
        if (!SubscriptionRegistry.TryAdd(name, out var subscription))
        {
            throw new InvalidOperationException($"There is already a subscription with the name {name}");
        }

        return new SubscriptionBuilder(subscription);
    }

    public void Map<TMessage>(string name) => MessageTypeMap.Map<TMessage>(name);

    public void Map(Type type, string name) => MessageTypeMap.Map(type, name);

    public void AddRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message
    ) where TMessage : notnull
    {
        if (!RecurringMessageRegistry.TryAdd(name, out var recurringMessage))
        {
            throw new InvalidOperationException($"There is already a recurring message with the name {name}");
        }

        if (message is not Message messageToAdd)
        {
            messageToAdd = new Message(message);
        }

        recurringMessage.CronExpression = cronExpression;
        recurringMessage.StreamName = streamName;
        recurringMessage.Message = messageToAdd;
    }
}
