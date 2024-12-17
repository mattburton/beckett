using Beckett.Subscriptions;

namespace Beckett.Configuration;

public class SubscriptionSettingsBuilder(Subscription subscription) : ISubscriptionSettingsBuilder
{
    public ISubscriptionSettingsBuilder HandlerName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        subscription.HandlerName = name;

        return this;
    }

    public ISubscriptionSettingsBuilder StartingPosition(StartingPosition startingPosition)
    {
        subscription.StartingPosition = startingPosition;

        return this;
    }

    public ISubscriptionSettingsBuilder MaxRetryCount(int maxRetryCount)
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        subscription.MaxRetriesByExceptionType[typeof(Exception)] = maxRetryCount;

        return this;
    }

    public ISubscriptionSettingsBuilder MaxRetryCount<TException>(int maxRetryCount)
        where TException : Exception
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        subscription.MaxRetriesByExceptionType[typeof(TException)] = maxRetryCount;

        return this;
    }

    public ISubscriptionSettingsBuilder Priority(int priority)
    {
        subscription.Priority = priority;

        return this;
    }
}
