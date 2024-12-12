using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class MessageSubscriptionBuilder(Subscription subscription) : IMessageSubscriptionBuilder
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

    public ISubscriptionConfigurationBuilder Handler<THandlerServiceType>(
        Func<THandlerServiceType, IMessageContext, CancellationToken, Task> handler,
        Type handlerImplementationType
    )
    {
        subscription.HandlerType = handlerImplementationType;
        subscription.HandlerName = handlerImplementationType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler((THandlerServiceType)h, (IMessageContext)c, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }
}

public class MessageSubscriptionBuilder<T>(Subscription subscription)
    : IMessageSubscriptionBuilder<T>
{
    public IMessageSubscriptionBuilder<TMessage> Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return new MessageSubscriptionBuilder<TMessage>(subscription);
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
        subscription.HandlerFunction = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler<THandlerServiceType>(
        Func<THandlerServiceType, IMessageContext, CancellationToken, Task> handler,
        Type handlerImplementationType
    )
    {
        subscription.HandlerType = handlerImplementationType;
        subscription.HandlerName = handlerImplementationType.FullName;
        subscription.HandlerFunction = (h, c, t) => handler((THandlerServiceType)h, (IMessageContext)c, t);

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

    public ISubscriptionConfigurationBuilder Handler<THandlerServiceType>(
        Func<THandlerServiceType, IReadOnlyList<IMessageContext>, CancellationToken, Task> handler,
        Type handlerImplementationType
    )
    {
        subscription.HandlerType = handlerImplementationType;
        subscription.HandlerName = handlerImplementationType.FullName;
        subscription.BatchHandlerFunction = (h, b, t) => handler((THandlerServiceType)h, b, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
