namespace Beckett.Configuration;

public interface IMessageSubscriptionBuilder
{
    /// <summary>
    /// Subscribe to a specific message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    /// <summary>
    /// Subscribe to a specific message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Message(Type messageType);

    /// <summary>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes <see cref="IMessageContext"/> and
    /// <see cref="CancellationToken"/> as inputs and returns a <see cref="Task"/> as the result. Any exceptions thrown
    /// by the handler will result in Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );

    /// <summary>
    /// Configure the handler for this subscription as a static function reference. The handler type does not need to
    /// be registered in the container since it is a static function. The handler is registered as a func that takes
    /// <see cref="IMessageContext"/> and <see cref="CancellationToken"/> as inputs and returns a <see cref="Task"/>
    /// as the result. The handler name must be specified to provide a display name for the static function in traces
    /// and logs - attempting to infer a useful name from a static function reference may or may not result in the
    /// desired outcome, so we are making it explicit. If you need to resolve services at runtime you can use the
    /// <see cref="IServiceProvider"/> provided by the <see cref="IMessageContext"/> <c>Services</c> property. Any
    /// exceptions thrown by the handler will result in Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <param name="handlerName">Handler display name for traces and logs</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler(
        Func<IMessageContext, CancellationToken, Task> handler,
        string handlerName
    );
}

public interface IMessageSubscriptionBuilder<out TMessage>
{
    /// <summary>
    /// Subscribe to a specific message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder<T> Message<T>();

    /// <summary>
    /// <para>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes <typeparamref name="TMessage"/> and
    /// <see cref="CancellationToken"/> as inputs and returns a <see cref="Task"/> as the result. Any exceptions thrown
    /// by the handler will result in Beckett retrying the subscription based on its configuration.
    /// </para>
    /// <para>
    /// Note that this handler registration is strongly typed in terms of the message type it handles. As such only one
    /// message type can be configured for this subscription. If multiple message types have been configured an
    /// exception will be thrown and the host will fail to start.
    /// </para>
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(Func<THandler, TMessage, CancellationToken, Task> handler);

    /// <summary>
    /// <para>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes <typeparamref name="TMessage"/>,
    /// <see cref="IMessageContext"/> and <see cref="CancellationToken"/> as inputs and returns a <see cref="Task"/>
    /// as the result. The <see cref="IMessageContext"/> is useful to access the message context - stream position,
    /// global position, etc... as well as the message metadata. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </para>
    /// <para>
    /// Note that this handler registration is strongly typed in terms of the message type it handles. As such only one
    /// message type can be configured for this subscription. If multiple message types have been configured an
    /// exception will be thrown and the host will fail to start.
    /// </para>
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, TMessage, IMessageContext, CancellationToken, Task> handler
    );

    /// <summary>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes <see cref="IMessageContext"/> and
    /// <see cref="CancellationToken"/> as inputs and returns a <see cref="Task"/> as the result. Any exceptions thrown
    /// by the handler will result in Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );
}
