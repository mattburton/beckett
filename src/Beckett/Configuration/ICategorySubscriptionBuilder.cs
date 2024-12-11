namespace Beckett.Configuration;

public interface ICategorySubscriptionBuilder
{
    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to for this category</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ICategorySubscriptionBuilder<TMessage> Message<TMessage>();

    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    ICategorySubscriptionBuilder Message(Type messageType);

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
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes a batch of messages and <see cref="CancellationToken"/> as
    /// inputs and returns a <see cref="Task"/> as the result. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IReadOnlyList<IMessageContext>, CancellationToken, Task> handler
    );
}

public interface ICategorySubscriptionBuilder<out TMessage>
{
    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to for this category</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ICategorySubscriptionBuilder<T> Message<T>();

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

    /// <summary>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. The handler is registered as a func that takes a batch of messages and <see cref="CancellationToken"/> as
    /// inputs and returns a <see cref="Task"/> as the result. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">Handler func</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IReadOnlyList<IMessageContext>, CancellationToken, Task> handler
    );
}
