using System.Text.Json.Nodes;
using Beckett.Messages;
using Beckett.Scheduling;
using Beckett.Subscriptions.Configuration;

namespace Beckett;

public interface IBeckettBuilder
{
    /// <summary>
    /// Explicitly map a message type to the name that will be stored as the type in the message store. When reading a
    /// message from the store Beckett will use the mapping during deserialization to map the message to the correct
    /// type at runtime.
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

    /// <summary>
    /// Creates an <see cref="ISubscriptionGroupBuilder"/> that allows you to configure subscriptions for a group
    /// </summary>
    /// <param name="groupName"></param>
    /// <returns></returns>
    ISubscriptionGroupBuilder MapGroup(string groupName);

    /// <summary>
    /// Add a subscription to the default subscription group hosted by this application and configure it using the
    /// resulting <see cref="ISubscriptionConfigurationBuilder"/>. If there is more than one group configured for the
    /// host you must use <see cref="MapGroup"/> to configure subscriptions per group.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ISubscriptionConfigurationBuilder AddSubscription(string name);

    /// <summary>
    /// <para>
    /// Schedule a recurring message. This is useful for scheduled tasks or other activities that occur on a regular
    /// interval. Recurring messages have a cron expression that configures the schedule. When the next occurrence
    /// comes due the supplied message will be appended to the specified stream. By subscribing to the message type you
    /// can then support scheduled tasks / jobs within your application using the same stream-based processing you are
    /// using elsewhere, including retries.
    /// </para>
    /// <para>
    /// Note that recurring messages will be written to the message store based on the interval specified by the cron
    /// expression you supply. If your application has not handled the previous message before the next occurrence comes
    /// due then they might overlap. If this is a concern we recommend using the <see cref="IMessageScheduler"/>
    /// instead to implement scheduled tasks where a subscription handler is responsible for scheduling the next
    /// occurrence manually once the current one succeeds.
    /// </para>
    /// <para>
    /// The cron expression will be parsed and validated to ensure it is a valid expression. Please see the Cronos
    /// <see href="https://github.com/HangfireIO/Cronos#cron-format">documentation</see> for specific cron expression
    /// feature support.
    /// </para>
    /// </summary>
    /// <param name="name">The name of the recurring message</param>
    /// <param name="cronExpression">The cron expression for the recurring message</param>
    /// <param name="timeZone">The time zone used to evaluate the cron schedule</param>
    /// <param name="streamName">The stream to append the message to</param>
    /// <param name="message">The message to send</param>
    /// <typeparam name="TMessage"></typeparam>
    void ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        TimeZoneInfo timeZone,
        string streamName,
        TMessage message
    ) where TMessage : class;
}

public class BeckettBuilder(BeckettOptions options) : IBeckettBuilder
{
    public void Map<TMessage>(string name) => MessageTypeMap.Map<TMessage>(name);

    public void Map(Type type, string name) => MessageTypeMap.Map(type, name);

    public void Upcast(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> upcaster) =>
        MessageUpcaster.Register(oldTypeName, newTypeName, upcaster);

    public ISubscriptionGroupBuilder MapGroup(string groupName)
    {
        var group = options.Subscriptions.Groups.FirstOrDefault(x => x.Name == groupName);

        if (group == null)
        {
            throw new InvalidOperationException(
                $"There is no subscription group named {groupName} configured for this host."
            );
        }

        return new SubscriptionGroupBuilder(group);
    }

    public ISubscriptionConfigurationBuilder AddSubscription(string name)
    {
        switch (options.Subscriptions.Groups.Count)
        {
            case 0:
                throw new InvalidOperationException(
                    "You must configure at least one subscription group before configuring subscriptions."
                );
            case > 1:
                throw new InvalidOperationException(
                    "There is more than one subscription group configured for this host - you must use builder.MapGroup to configure subscriptions per group."
                );
        }

        var group = options.Subscriptions.Groups[0];

        if (!group.TryAddSubscription(name, out var subscription))
        {
            throw new InvalidOperationException(
                $"There is already a subscription with the name {name} in the group {group.Name}"
            );
        }

        return new SubscriptionConfigurationBuilder(subscription);
    }

    public void ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        TimeZoneInfo timeZone,
        string streamName,
        TMessage message
    ) where TMessage : class
    {
        if (!RecurringMessageRegistry.TryAdd(name, out var recurringMessage))
        {
            throw new InvalidOperationException($"There is already a recurring message with the name {name}");
        }

        if (message is not Message envelope)
        {
            envelope = new Message(message);
        }

        recurringMessage.CronExpression = cronExpression;
        recurringMessage.TimeZoneId = timeZone.Id;
        recurringMessage.StreamName = streamName;
        recurringMessage.Message = envelope;
    }
}
