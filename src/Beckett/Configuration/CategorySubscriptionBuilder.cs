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
