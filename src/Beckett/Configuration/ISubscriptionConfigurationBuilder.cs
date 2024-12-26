using Beckett.Projections;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Configuration;

public interface ISubscriptionConfigurationBuilder
{
    /// <summary>
    /// <para>
    /// Optional - only subscribe to messages appended to streams in a given category. Example:
    /// </para>
    /// <para>
    /// Order messages are appended to streams named like <c>Order-1234</c>. In this case the category is "Order" using
    /// the default convention that anything before the first dash in a stream name is the category of the stream. If
    /// you wanted to constrain the subscription to only react to messages appended to the "Order" category you could
    /// configure that by supplying the category name to this method.
    /// </para>
    /// <para>
    /// This is also useful when you wish to subscribe to all messages appended to streams of a given category
    /// regardless of their type.
    /// </para>
    /// </summary>
    /// <param name="category">The stream category to constrain this subscription to</param>
    /// <returns></returns>
    ICategorySubscriptionBuilder Category(string category);

    /// <summary>
    /// Subscribe your handler to a specific message type. When this message type is read from the global stream your
    /// subscription will be invoked. You can configure multiple message types for a single subscription as well using
    /// the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    /// <summary>
    /// Subscribe your handler to a specific message type. When this message type is read from the global stream your
    /// subscription will be invoked. You can configure multiple message types for a single subscription as well using
    /// the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Message(Type messageType);

    /// <summary>
    /// Subscribe your handler to multiple message types. When any of these message types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Messages(params Type[] messageTypes) => Messages(messageTypes.AsEnumerable());

    /// <summary>
    /// Subscribe your handler to multiple message types. When any of these message types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionBuilder Messages(IEnumerable<Type> messageTypes);

    /// <summary>
    /// Configure the handler for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="serviceLifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="TProjection">Projection handler</typeparam>
    /// <typeparam name="TReadModel">Read model</typeparam>
    /// <typeparam name="TKey">Read model key</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionSettingsBuilder Projection<TProjection, TReadModel, TKey>(
        ServiceLifetime serviceLifetime = ServiceLifetime.Transient
    ) where TProjection : IProjection<TReadModel, TKey> where TReadModel : IApply, new();
}
