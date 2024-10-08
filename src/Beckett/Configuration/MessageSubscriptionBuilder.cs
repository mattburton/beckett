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
        subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public ISubscriptionConfigurationBuilder Handler(
        Func<IMessageContext, CancellationToken, Task> handler,
        string handlerName
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

        subscription.HandlerName = handlerName;
        subscription.StaticMethod = (c, t) => handler((IMessageContext)c, t);

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
        subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (T)((IMessageContext)c).Message!, t);

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
        subscription.InstanceMethod = (h, c, t) => handler(
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
        subscription.InstanceMethod = (h, c, t) => handler(
            (THandler)h,
            (IMessageContext)c,
            t
        );

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
