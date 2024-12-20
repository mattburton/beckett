using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Configuration;

public interface IMessageSubscriptionBuilder<out TMessage>
{
    /// <summary>
    /// Subscribe to an additional message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder And<T>();

    /// <summary>
    /// Configure the handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Handler<THandler>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where THandler : IMessageHandler<TMessage>;

    /// <summary>
    /// Configure the handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder BatchHandler<THandler>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where THandler : IMessageBatchHandler;
}

public interface IMessageSubscriptionBuilder
{
    /// <summary>
    /// Subscribe to an additional message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder And<TMessage>();

    /// <summary>
    /// Subscribe to a specific message type for this subscription. When this message type is read from the global
    /// stream your subscription will be invoked. You can configure multiple message types for a single subscription as
    /// well using the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder And(Type messageType);

    /// <summary>
    /// Configure the handler for this subscription. The handler type <typeparamref name="THandler"/> must be a
    /// registered service in the host as it will be resolved within a scope surrounding the execution of the handler
    /// itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been registered as
    /// well. Any exceptions thrown by the handler will result in Beckett retrying the subscription based on its
    /// configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Handler<THandler>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient) where THandler : IMessageHandler;

    /// <summary>
    /// Configure the handler for this subscription. The handler type must implement <see cref="IMessageHandler"/> and
    /// be a registered service in the host as it will be resolved within a scope surrounding the execution of the
    /// handler itself. Beckett will perform a diagnostic check at startup to ensure that the handler has been
    /// registered as well. Any exceptions thrown by the handler will result in Beckett retrying the subscription based
    /// on its configuration.
    /// </summary>
    /// <param name="handlerType">The handler type</param>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Handler(Type handlerType, ServiceLifetime serviceLifetime = ServiceLifetime.Transient);
}
