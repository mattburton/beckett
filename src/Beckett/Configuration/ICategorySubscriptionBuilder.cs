using Microsoft.Extensions.DependencyInjection;

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
    /// Configure the handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Handler<THandler>(ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        where THandler : IMessageHandler;

    /// <summary>
    /// Configure the handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handlerType">Handler type</param>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Handler(
        Type handlerType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    );

    /// <summary>
    /// Configure the batch handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder BatchHandler<THandler>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where THandler : IMessageBatchHandler;

    /// <summary>
    /// Configure the batch handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder BatchHandler(
        Type handlerType,
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    );
}
