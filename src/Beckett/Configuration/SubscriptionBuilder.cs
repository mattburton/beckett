using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class SubscriptionBuilder(Subscription subscription) : ISubscriptionBuilder
{
    public ICategorySubscriptionBuilder Category(string category)
    {
        subscription.Category = category;

        return new CategorySubscriptionBuilder(subscription);
    }

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
}
