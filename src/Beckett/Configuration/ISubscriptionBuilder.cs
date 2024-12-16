namespace Beckett.Configuration;

public interface ISubscriptionBuilder
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
    IMessageSubscriptionTypedBuilder<TMessage> Message<TMessage>();

    /// <summary>
    /// Subscribe your handler to a specific message type. When this message type is read from the global stream your
    /// subscription will be invoked. You can configure multiple message types for a single subscription as well using
    /// the fluent builder returned by this method.
    /// </summary>
    /// <param name="messageType">The message type to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionUntypedBuilder Message(Type messageType);

    /// <summary>
    /// Subscribe your handler to multiple message types. When any of these message types are read from the global
    /// stream your subscription will be invoked.
    /// </summary>
    /// <param name="messageTypes">The message types to subscribe to</param>
    /// <returns>Builder to further configure the subscription</returns>
    IMessageSubscriptionUntypedBuilder Messages(IEnumerable<Type> messageTypes);
}
