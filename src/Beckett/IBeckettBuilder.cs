using System.Text.Json.Nodes;
using Beckett.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public interface IBeckettBuilder : IMessageTypeBuilder, ISubscriptionBuilder;

public interface IMessageTypeBuilder
{
    /// <summary>
    /// Explicitly map a message type to the name that will be stored as the type in the message store. When reading a
    /// message from the store Beckett will use the mapping during deserialization to map the message to the correct
    /// type at runtime. This is primarily used when <c>BeckettOptions.Messages.AllowDynamicTypeMapping</c> is
    /// set to false, but can be used in conjunction with that setting to override mapping as necessary.
    /// </summary>
    /// <param name="name">The message type name</param>
    /// <typeparam name="TMessage">The message type</typeparam>
    void Map<TMessage>(string name);

    /// <summary>
    /// Explicitly map a message type to the name that will be stored as the type in the message store. When reading a
    /// message from the store Beckett will use the mapping during deserialization to map the message to the correct
    /// type at runtime. This is primarily used when <c>BeckettOptions.Messages.AllowDynamicTypeMapping</c> is
    /// set to false, but can be used in conjunction with that setting to override mapping as necessary.
    /// </summary>
    /// <param name="type">The message type</param>
    /// <param name="name">The message type name</param>
    void Map(Type type, string name);

    /// <summary>
    /// Upcast the name and /or schema of a message from an old type to a new type. Using a <see cref="JsonObject"/>
    /// makes it straightforward to add and remove properties and otherwise transform message payloads as necessary.
    /// If you are just renaming a message type without changing the schema you can register a no-op upcaster, i.e.
    /// <c>x => x</c>. Upcasters are recursive, meaning you can register upcasters for v1 -> v2, v2 -> v3, and v3 -> v4.
    /// When the message type v1 is read from message storage it will be transformed to v4 using the registered
    /// upcasters and apply them in order. Alternatively you could just register a single upcaster for v1 -> v4 if
    /// that's more appropriate.
    /// </summary>
    /// <param name="oldTypeName">The old type name</param>
    /// <param name="newTypeName">The new type name</param>
    /// <param name="upcaster">The upcaster function</param>
    void Upcast(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> upcaster);
}

public interface ISubscriptionBuilder
{
    /// <summary>
    /// Host service collection to register additional dependencies required by the application module
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Add a subscription to the subscription group hosted by this application and configure it using the resulting
    /// <see cref="ISubscriptionConfigurationBuilder"/>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ISubscriptionConfigurationBuilder AddSubscription(string name);
}
