namespace Beckett.Configuration;

public interface ICategorySubscriptionBuilder
{
    ICategorySubscriptionBuilder<TMessage> Message<TMessage>();

    ICategorySubscriptionBuilder Message(Type messageType);

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler(
        Func<IMessageContext, CancellationToken, Task> handler,
        string handlerName
    );
}

public interface ICategorySubscriptionBuilder<out TMessage>
{
    ICategorySubscriptionBuilder<T> Message<T>();

    ISubscriptionConfigurationBuilder Handler<THandler>(Func<THandler, TMessage, CancellationToken, Task> handler);

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, TMessage, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );
}
