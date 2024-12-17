using Beckett.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Configuration;

public class SubscriptionConfigurationBuilder(Subscription subscription, IServiceCollection services) : ISubscriptionConfigurationBuilder
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
}
