using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class SubscriptionConfigurationBuilder(Subscription subscription) : ISubscriptionConfigurationBuilder
{
    public ISubscriptionConfigurationBuilder HandlerName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        subscription.HandlerName = name;

        return this;
    }

    public ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition)
    {
        subscription.StartingPosition = startingPosition;

        return this;
    }

    public ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount)
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        subscription.MaxRetriesByExceptionType[typeof(Exception)] = maxRetryCount;

        return this;
    }

    public ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount)
        where TException : Exception
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        subscription.MaxRetriesByExceptionType[typeof(TException)] = maxRetryCount;

        return this;
    }

    public ISubscriptionConfigurationBuilder Priority(int priority)
    {
        subscription.Priority = priority;

        return this;
    }
}
