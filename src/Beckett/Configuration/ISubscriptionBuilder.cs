namespace Beckett.Configuration;

public interface ISubscriptionBuilder
{
    ICategorySubscriptionBuilder Category(string category);

    IMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    IMessageSubscriptionBuilder Message(Type messageType);
}
