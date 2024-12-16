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
    IMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Message(Type messageType);

    /// <summary>
    /// Subscribe to multiple message types within this category. When any of these types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Messages(params Type[] messageTypes) => Messages(messageTypes.AsEnumerable());

    /// <summary>
    /// Subscribe to multiple message types within this category. When any of these types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Messages(IEnumerable<Type> messageTypes);

    /// <summary>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. Any exceptions thrown by the handler will result in Beckett retrying the subscription based on its
    /// configuration.
    /// </summary>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler<THandler>() where THandler : IMessageHandler;

    /// <summary>
    /// Configure the handler for this subscription. TThe handler type must implement <see cref="IMessageHandler"/> and
    /// be a registered service in the host as it will be resolved within a scope surrounding the execution of the
    /// handler itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been
    /// registered as well. Any exceptions thrown by the handler will result in Beckett retrying the subscription based
    /// on its configuration.
    /// </summary>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler(Type handlerType);

    /// <summary>
    /// Configure the batch handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. Any exceptions thrown by the handler will result in Beckett retrying the subscription based on its
    /// configuration.
    /// </summary>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder BatchHandler<THandler>() where THandler : IMessageBatchHandler;

    /// <summary>
    /// Configure the batch handler for this subscription. The handler type must implement
    /// <see cref="IMessageBatchHandler"/> and be a registered service in the host as it will be resolved within a scope
    /// surrounding the execution of the handler itself. Beckett will perform a diagnostic check at startup to ensure
    /// that the handler has been registered as well. Any exceptions thrown by the handler will result in Beckett
    /// retrying the subscription based on its configuration.
    /// </summary>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder BatchHandler(Type handlerType);
}
