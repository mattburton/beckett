using System.Runtime.CompilerServices;
using Beckett.Projections;
using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Beckett.Configuration;

public class SubscriptionConfigurationBuilder(Subscription subscription, IServiceCollection services)
    : ISubscriptionConfigurationBuilder
{
    public ICategorySubscriptionBuilder Category(string category)
    {
        subscription.Category = category;

        return new CategorySubscriptionBuilder(subscription, services);
    }

    public IMessageSubscriptionBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionBuilder<TMessage>(subscription, services);
    }

    public IMessageSubscriptionBuilder Message(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new MessageSubscriptionBuilder(subscription, services);
    }

    public IMessageSubscriptionBuilder Messages(IEnumerable<Type> messageTypes)
    {
        foreach (var messageType in messageTypes)
        {
            subscription.RegisterMessageType(messageType);
        }

        return new MessageSubscriptionBuilder(subscription, services);
    }

    public ISubscriptionSettingsBuilder Projection<TProjection, TState, TKey>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where TProjection : IProjection<TState, TKey> where TState : IApply, new()
    {
        var handlerType = typeof(TProjection);

        services.TryAdd(new ServiceDescriptor(handlerType, handlerType, serviceLifetime));

        subscription.BatchHandler = true;
        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        var projection = (IProjection<TState, TKey>)RuntimeHelpers.GetUninitializedObject(typeof(TProjection));

        var configuration = new ProjectionConfiguration<TKey>();

        projection.Configure(configuration);

        Messages(configuration.GetMessageTypes());

        return new SubscriptionSettingsBuilder(subscription);
    }
}
