namespace Beckett.Subscriptions.Configuration;

public class SubscriptionGroupBuilder(SubscriptionGroup group) : ISubscriptionGroupBuilder
{
    public ISubscriptionConfigurationBuilder AddSubscription(string name)
    {
        if (!group.TryAddSubscription(name, out var subscription))
        {
            throw new InvalidOperationException(
                $"There is already a subscription with the name {name} in the group {group.Name}"
            );
        }

        return new SubscriptionConfigurationBuilder(subscription);
    }
}
