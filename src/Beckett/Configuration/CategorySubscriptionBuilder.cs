using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Beckett.Configuration;

public class CategorySubscriptionBuilder(Subscription subscription, IServiceCollection services)
    : ICategorySubscriptionBuilder
{
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

    public ISubscriptionSettingsBuilder Handler<THandler>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where THandler : IMessageHandler
    {
        var handlerType = typeof(THandler);

        return Handler(handlerType, serviceLifetime);
    }

    public ISubscriptionSettingsBuilder Handler(
        Type handlerType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    )
    {
        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException(
                $"Message handler {handlerType.FullName} does not implement {nameof(IMessageHandler)}"
            );
        }

        services.TryAdd(new ServiceDescriptor(handlerType, handlerType, serviceLifetime));

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionSettingsBuilder(subscription);
    }

    public ISubscriptionSettingsBuilder BatchHandler<THandler>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where THandler : IMessageBatchHandler
    {
        var handlerType = typeof(THandler);

        return BatchHandler(handlerType);
    }

    public ISubscriptionSettingsBuilder BatchHandler(
        Type handlerType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    )
    {
        if (!typeof(IMessageBatchHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException(
                $"Batch handler {handlerType.FullName} does not implement {nameof(IMessageBatchHandler)}"
            );
        }

        services.TryAdd(new ServiceDescriptor(handlerType, handlerType, serviceLifetime));

        subscription.BatchHandler = true;
        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionSettingsBuilder(subscription);
    }
}
