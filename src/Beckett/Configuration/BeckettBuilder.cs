using System.Text.Json.Nodes;
using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Configuration;

public class BeckettBuilder(IServiceCollection services) : IBeckettBuilder
{
    public IServiceCollection Services { get; } = services;

    public ISubscriptionConfigurationBuilder AddSubscription(string name)
    {
        if (!SubscriptionRegistry.TryAdd(name, out var subscription))
        {
            throw new InvalidOperationException($"There is already a subscription with the name {name}");
        }

        return new SubscriptionConfigurationBuilder(subscription, Services);
    }

    public void Map<TMessage>(string name) => MessageTypeMap.Map<TMessage>(name);

    public void Map(Type type, string name) => MessageTypeMap.Map(type, name);

    public void Upcast(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> upcaster) =>
        MessageUpcaster.Register(oldTypeName, newTypeName, upcaster);
}
