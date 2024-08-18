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
    IServiceCollection services,
    IMessageTypeMap messageTypeMap,
    ISubscriptionRegistry subscriptionRegistry,
    IRecurringMessageRegistry recurringMessageRegistry
) : IBeckettBuilder
{
    public IConfiguration Configuration { get; } = configuration;
    public IHostEnvironment Environment { get; } = environment;
    public IServiceCollection Services { get; } = services;

    public ISubscriptionBuilder AddSubscription(string name)
    {
        if (!subscriptionRegistry.TryAdd(name, out var subscription))
        {
            throw new InvalidOperationException($"There is already a subscription with the name {name}");
        }

        return new SubscriptionBuilder(subscription);
    }

    public IBeckettBuilder Build(Action<IBeckettBuilder> build)
    {
        build(this);

        return this;
    }

    public void Map<TMessage>(string name) => messageTypeMap.Map<TMessage>(name);

    public void Map(Type type, string name) => messageTypeMap.Map(type, name);

    public void AddRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        Dictionary<string, object>? metadata = null
    ) where TMessage : notnull
    {
        if (!recurringMessageRegistry.TryAdd(name, out var recurringMessage))
        {
            throw new InvalidOperationException($"There is already a recurring message with the name {name}");
        }

        recurringMessage.CronExpression = cronExpression;
        recurringMessage.StreamName = streamName;
        recurringMessage.Message = message;
        recurringMessage.Metadata = metadata ?? new Dictionary<string, object>();
    }
}
