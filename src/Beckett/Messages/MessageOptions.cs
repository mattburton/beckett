namespace Beckett.Messages;

public class MessageOptions
{
    /// <summary>
    /// Configure whether to allow dynamic type mapping within Beckett. If disabled then all message types must be
    /// mapped to the name that will be used when reading and writing to the message store. When this is disabled
    /// and a message type that hasn't been mapped is configured for a subscription an exception will be thrown.
    /// If enabled message types will be mapped automatically based on their type name without the namespace, so
    /// <c>AcmeCo.Events.OrderCreated</c> would be mapped as <c>OrderCreated</c>. Message type mappings are globally
    /// unique as well, so if more than one type is named <c>OrderCreated</c> that will result in an exception being
    /// thrown. This setting is enabled by default to allow you to get up and running quickly, but we recommend
    /// disabling it in practice.
    /// </summary>
    public bool AllowDynamicTypeMapping { get; set; } = true;

    /// <summary>
    /// Configure how Beckett handles unknown message types. Message types can get deleted over time if they are no
    /// longer in use, etc... so this setting allows you to pick the appropriate policy for your application. By
    /// default, Beckett will use the <c>LogErrorAndContinue</c> policy which will log an error and continue processing
    /// new messages, but you can select other policies as well. If an unknown message type is an error condition that
    /// warrants investigation then select the <c>LogErrorAndExitApplication</c> policy to log an error and cause the
    /// host application to shut down. Otherwise select <c>IgnoreAndContinue</c> to simply skip the unknown type and
    /// continue processing.
    /// </summary>
    public UnknownMessageTypePolicy UnknownMessageTypePolicy { get; set; } =
        UnknownMessageTypePolicy.LogErrorAndContinue;
}

public enum UnknownMessageTypePolicy
{
    IgnoreAndContinue,
    LogErrorAndContinue,
    LogErrorAndExitApplication
}
