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
    ISubscriptionConfigurationBuilder Category(string category);

    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <typeparam name="TMessage">The message type to subscribe to for this category</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Message<TMessage>();

    /// <summary>
    /// Subscribe to a specific message type within this category. When this message type is read from the global stream
    /// your subscription will be invoked. You can configure multiple message types for a single subscription as well
    /// using the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Message(Type messageType);

    /// <summary>
    /// Subscribe to multiple message types within this category. When any of these types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Messages(params Type[] messageTypes) => Messages(messageTypes.AsEnumerable());

    /// <summary>
    /// Subscribe to multiple message types within this category. When any of these types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to for this category</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Messages(IEnumerable<Type> messageTypes);

    /// <summary>
    /// Configure the handler for this subscription. Handler methods can be a lambda expression, a local function, an
    /// instance method or a static method. The parameters will determine the type of handler - if the method accepts
    /// an <see cref="IReadOnlyList{T}"/> of type <see cref="IMessageContext"/> or <see cref="object"/> then it is a
    /// batch handler, otherwise it handles individual messages via <see cref="IMessageContext"/> and / or a typed
    /// message instance. Handler methods are async and must return <see cref="Task"/> and therefore can accept a
    /// <see cref="CancellationToken"/> parameter as well. Dependency injection is also supported - any other parameters
    /// will be resolved from a per-handler service scope. Any exceptions thrown by the handler will result in Beckett
    /// retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="handler">The subscription handler</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Handler(Delegate handler);

    /// <summary>
    /// Optional - configure the handler name for this subscription. If set the handler name is included in Open
    /// Telemetry traces as a tag when the handler is executed. If a static handler method is used then the name will
    /// automatically be set based on the full type name of the class containing the method plus the name of the method
    /// itself, and can be overridden using this method. For all other kinds of handler methods the handler name will
    /// not be set automatically, so if you would like to have a name included in the traces then you will need to set
    /// one manually.
    /// </summary>
    /// <param name="handlerName">The subscription handler name</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder HandlerName(string handlerName);

    /// <summary>
    /// Configure a projection for this subscription which will be registered in the container using the specified
    /// service lifetime unless it has already been registered. Any exceptions thrown by the handler will result in
    /// Beckett retrying the subscription based on its configuration.
    /// </summary>
    /// <param name="lifetime">Service lifetime to use when registering the handler in the container</param>
    /// <typeparam name="TProjection">Projection handler</typeparam>
    /// <typeparam name="TReadModel">Read model</typeparam>
    /// <typeparam name="TKey">Read model key</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Projection<TProjection, TReadModel, TKey>(
        ServiceLifetime lifetime = ServiceLifetime.Transient
    ) where TProjection : IProjection<TReadModel, TKey> where TReadModel : IApply, new();

    /// <summary>
    /// Configure the starting position of the subscription. When adding a new subscription to an existing system this
    /// will determine whether the subscription starts from the beginning of the message store -
    /// <c>StartingPosition.Earliest</c> - processing all past messages as it gets caught up or if it only cares about
    /// new messages - <c>StartingPosition.Latest</c>. Typically, read models / projections fall into the former
    /// category and event handlers / reactors fall in the latter category.
    /// </summary>
    /// <param name="startingPosition">Starting position for the subscription</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition);

    /// <summary>
    /// Configure the default max retry count for this subscription. This will override the max retry count configured
    /// at the host-level in the Beckett subscription options for just this subscription, and can be overridden by
    /// configuring the max retry count for specific exception types using <see cref="MaxRetryCount{TException}"/>.
    /// </summary>
    /// <param name="maxRetryCount">Max retry count</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount);

    /// <summary>
    /// Configure the max retry count for a specific exception type for this subscription. This is useful in scenarios
    /// where you have known exceptions that should lead to specific retry behavior - i.e. if a given exception being
    /// thrown means that there is no chance that retrying this subscription will lead to a successful outcome then
    /// we can set the max retry count for that exception type to zero, meaning that it will not be retried and the
    /// status of the checkpoint will be set to failed immediately. In this scenario the failure will be visible in the
    /// list of failed retries on the Beckett dashboard. If the max retry count for the same exception type has been
    /// configured at the host-level in the Beckett subscription options setting it here will override that value for
    /// this subscription.
    /// </summary>
    /// <param name="maxRetryCount">Max retry count</param>
    /// <typeparam name="TException">Exception type</typeparam>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount) where TException : Exception;

    /// <summary>
    /// Configure the priority for this subscription. When messages are available for multiple subscriptions at the same
    /// time they will be sorted based on their priority, lowest to highest. This affects the order in which the
    /// resulting lagging checkpoints will be processed. Defaults to <c>int.MaxValue</c>.
    /// </summary>
    /// <param name="priority">Priority</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder Priority(int priority);
}
