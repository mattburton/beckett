namespace Beckett.Configuration;

public interface ISubscriptionConfigurationBuilder
{
    /// <summary>
    /// Configure the handler name for this subscription. If specified this will override the handler name that was
    /// configured previously or inferred from the handler type name. If omitted, for non-static handlers the full type
    /// name of the handler will be used as the handler name for the subscription.
    /// </summary>
    /// <param name="name">Handler name</param>
    /// <returns>Builder to further configure the subscription</returns>
    ISubscriptionConfigurationBuilder HandlerName(string name);

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
