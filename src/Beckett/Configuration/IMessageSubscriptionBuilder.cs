namespace Beckett.Configuration;

public interface IMessageSubscriptionBuilder
{
    IMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    IMessageSubscriptionBuilder Message(Type messageType);

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler(
        Func<IMessageContext, CancellationToken, Task> handler,
        string handlerName
    );
}

public interface IMessageSubscriptionBuilder<out TMessage>
{
    IMessageSubscriptionBuilder<T> Message<T>();

    ISubscriptionConfigurationBuilder Handler<THandler>(Func<THandler, TMessage, CancellationToken, Task> handler);

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, TMessage, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );
}
