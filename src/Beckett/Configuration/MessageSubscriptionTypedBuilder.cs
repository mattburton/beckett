using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class MessageSubscriptionTypedBuilder<T>(Subscription subscription) : IMessageSubscriptionTypedBuilder<T>
{
    public IMessageSubscriptionUntypedBuilder And<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionUntypedBuilder(subscription);
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
