using System.Text.Json.Nodes;
using Beckett.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett;

public interface IBeckettBuilder
{
    /// <summary>
    /// Host configuration to access application settings in your application module
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Host environment to access environment information in your application module
    /// </summary>
    IHostEnvironment Environment { get; }

    /// <summary>
    /// Host service collection to register additional dependencies required by your application module
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Add a subscription to the subscription group hosted by this application and configure it using the resulting
    /// <see cref="ISubscriptionBuilder"/>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ISubscriptionBuilder AddSubscription(string name);

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
    /// Map an old type name to a new type name. This is useful when a message type is renamed and you still need
    /// messages written using the old type name to map to the new type name - ex: some-event -> some-event.v2. This
    /// mapping is also recursive, so you can have mappings for multiple versions of the same type - v1 -> v2, v2 -> v3,
    /// and so on.
    /// </summary>
    /// <param name="oldTypeName">The old type name</param>
    /// <param name="newTypeName">The new type name</param>
    void Map(string oldTypeName, string newTypeName);

    /// <summary>
    /// Transform the schema of a message from an old type to a new type. Using a <see cref="JsonObject"/> makes it
    /// straightforward to add and remove properties and otherwise transform message payloads as necessary.
    /// Transformations are recursive, meaning you can register transformations for v1 -> v2, v2 -> v3, and v3 -> v4.
    /// When the message type v1 is read from message storage it will be transformed to v4 using the registered
    /// transformations and apply them in order. Alternatively you could just register a single transformation for
    /// v1 -> v4 if that's more appropriate.
    /// </summary>
    /// <param name="oldTypeName">The old type name</param>
    /// <param name="newTypeName">The new type name</param>
    /// <param name="transformation"></param>
    void Transform(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> transformation);
}
