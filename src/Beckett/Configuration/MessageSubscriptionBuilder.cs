using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class MessageSubscriptionBuilder<T>(Subscription subscription) : IMessageSubscriptionBuilder<T>
{
    public IMessageSubscriptionBuilder And<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>() where THandler : IMessageHandler<T>
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder BatchHandler<THandler>() where THandler : IMessageBatchHandler
    {
        var handlerType = typeof(THandler);

        subscription.BatchHandler = true;
        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionConfigurationBuilder(subscription);
    }
}

public class MessageSubscriptionBuilder(Subscription subscription) : IMessageSubscriptionBuilder
{
    public IMessageSubscriptionBuilder And<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionBuilder(subscription);
    }

    public IMessageSubscriptionBuilder And(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new MessageSubscriptionBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>() where THandler : IMessageHandler
    {
        var handlerType = typeof(THandler);

        return Handler(handlerType);
    }

    public ISubscriptionConfigurationBuilder Handler(Type handlerType)
    {
        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException($"Message handler {handlerType.FullName} does not implement {nameof(IMessageHandler)}");
        }

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
