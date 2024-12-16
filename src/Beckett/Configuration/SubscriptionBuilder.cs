using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class SubscriptionBuilder(Subscription subscription) : ISubscriptionBuilder
{
    public ICategorySubscriptionBuilder Category(string category)
    {
        subscription.Category = category;

        return new CategorySubscriptionBuilder(subscription);
    }

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
}
