using Beckett.Subscriptions;

namespace Beckett.Configuration;

public interface ISubscriptionConfigurationBuilder
{
    ISubscriptionConfigurationBuilder HandlerName(string name);
    ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition);
    ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount);
    ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount) where TException : Exception;
}
