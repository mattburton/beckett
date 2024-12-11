using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class CategorySubscriptionBuilder(Subscription subscription) : ICategorySubscriptionBuilder
{
    public ICategorySubscriptionBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new CategorySubscriptionBuilder<TMessage>(subscription);
    }

    public ICategorySubscriptionBuilder Message(Type messageType)
    {
        subscription.RegisterMessageType(messageType);

        return new CategorySubscriptionBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    )
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IReadOnlyList<IMessageContext>, CancellationToken, Task> handler
    )
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.BatchHandlerFunction = (h, b, t) => handler((THandler)h, b, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }
}

public class CategorySubscriptionBuilder<T>(Subscription subscription)
    : ICategorySubscriptionBuilder<T>
{
    public ICategorySubscriptionBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new CategorySubscriptionBuilder<TMessage>(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, T, CancellationToken, Task> handler
    )
    {
        subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler((THandler)h, (T)((IMessageContext)c).Message!, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, T, IMessageContext, CancellationToken, Task> handler
    )
    {
        subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler(
            (THandler)h,
            (T)((IMessageContext)c).Message!,
            (IMessageContext)c,
            t
        );

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    )
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler(
            (THandler)h,
            (IMessageContext)c,
            t
        );

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IReadOnlyList<IMessageContext>, CancellationToken, Task> handler
    )
    {
        var handlerType = typeof(THandler);

        subscription.HandlerType = handlerType;
        subscription.HandlerName = handlerType.FullName;
        subscription.BatchHandlerFunction = (h, b, t) => handler((THandler)h, b, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }

}
