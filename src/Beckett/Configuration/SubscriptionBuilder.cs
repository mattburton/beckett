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
        subscription.MessageTypes.Add(typeof(TMessage));

        return new MessageSubscriptionBuilder<TMessage>(subscription);
    }

    public IMessageSubscriptionBuilder Message(Type messageType)
    {
        subscription.MessageTypes.Add(messageType);

        return new MessageSubscriptionBuilder(subscription);
    }
}
