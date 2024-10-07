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
    /// <para>
    /// Add a recurring message. This is useful for scheduled tasks or other activities that occur on a regular
    /// interval. A recurring message has a name that is unique to the current subscription group, and a cron expression
    /// that configures the schedule. When the next occurrence comes due the supplied message will be appended to the
    /// specified stream. By subscribing to the message type you can then support scheduled tasks / jobs within your
    /// application using the same stream-based processing you are using elsewhere, including retries.
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
    /// <param name="streamName">The stream to append the message to</param>
    /// <param name="message">The message to send</param>
    void AddRecurringMessage(
        string name,
        string cronExpression,
        string streamName,
        Message message
    );
}

public static class BeckettBuilderExtensions
{
    /// <summary>
    /// <para>
    /// Add a recurring message. This is useful for scheduled tasks or other activities that occur on a regular
    /// interval. A recurring message has a name that is unique to the current subscription group, and a cron expression
    /// that configures the schedule. When the next occurrence comes due the supplied message will be appended to the
    /// specified stream. By subscribing to the message type you can then support scheduled tasks / jobs within your
    /// application using the same stream-based processing you are using elsewhere, including retries.
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
    /// <param name="builder">The Beckett builder</param>
    /// <param name="name">The name of the recurring message</param>
    /// <param name="cronExpression">The cron expression for the recurring message</param>
    /// <param name="streamName">The stream to append the message to</param>
    /// <param name="message">The message to send</param>
    /// <param name="metadata">Optional message metadata</param>
    /// <typeparam name="TMessage"></typeparam>
    public static void AddRecurringMessage<TMessage>(
        this IBeckettBuilder builder,
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        Dictionary<string, object>? metadata = null
    ) where TMessage : notnull => builder.AddRecurringMessage(name, cronExpression, streamName, new Message(message, metadata));
}
