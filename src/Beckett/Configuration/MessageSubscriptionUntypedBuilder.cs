using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class MessageSubscriptionUntypedBuilder(Subscription subscription) : IMessageSubscriptionUntypedBuilder
{
    public IMessageSubscriptionUntypedBuilder And<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionUntypedBuilder(subscription);
    }

    public IMessageSubscriptionUntypedBuilder And(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new MessageSubscriptionUntypedBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>() where THandler : IMessageHandler
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
