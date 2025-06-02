namespace Beckett.Subscriptions.Configuration;

public interface ISubscriptionGroupBuilder
{
    /// <summary>
    /// Add a subscription to the subscription group and configure it using the resulting
    /// <see cref="ISubscriptionConfigurationBuilder"/>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    ISubscriptionConfigurationBuilder AddSubscription(string name);
}
