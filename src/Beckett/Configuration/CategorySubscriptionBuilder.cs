using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class CategorySubscriptionBuilder(Subscription subscription) : ICategorySubscriptionBuilder
{
    public IMessageSubscriptionTypedBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionTypedBuilder<TMessage>(subscription);
    }

    public IMessageSubscriptionUntypedBuilder Message(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new MessageSubscriptionUntypedBuilder(subscription);
    }

    public IMessageSubscriptionUntypedBuilder Messages(IEnumerable<Type> messageTypes)
    {
        foreach (var messageType in messageTypes)
        {
            subscription.RegisterMessageType(messageType);
        }

        return new MessageSubscriptionUntypedBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>() where THandler : IMessageHandler
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
