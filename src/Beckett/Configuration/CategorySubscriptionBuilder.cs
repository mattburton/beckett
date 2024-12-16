using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class CategorySubscriptionBuilder(Subscription subscription) : ICategorySubscriptionBuilder
{
    public IMessageSubscriptionBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionBuilder<TMessage>(subscription);
    }

    public IMessageSubscriptionBuilder Message(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new MessageSubscriptionBuilder(subscription);
    }

    public IMessageSubscriptionBuilder Messages(IEnumerable<Type> messageTypes)
    {
        foreach (var messageType in messageTypes)
        {
            subscription.RegisterMessageType(messageType);
        }

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

    public ISubscriptionConfigurationBuilder BatchHandler<THandler>() where THandler : IMessageBatchHandler
    {
        var handlerType = typeof(THandler);

        return BatchHandler(handlerType);
    }

    public ISubscriptionConfigurationBuilder BatchHandler(Type handlerType)
    {
        if (!typeof(IMessageBatchHandler).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException($"Batch handler {handlerType.FullName} does not implement {nameof(IMessageBatchHandler)}");
        }

        subscription.BatchHandler = true;
        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
