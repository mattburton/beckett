using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Configuration;

public class SubscriptionConfigurationBuilder(
    Subscription subscription,
    IServiceCollection services
) : ISubscriptionConfigurationBuilder
{
    public IServiceCollection Services => services;

    public Subscription Subscription => subscription;

    public ISubscriptionConfigurationBuilder Category(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        subscription.Category = category;

        return this;
    }

    public ISubscriptionConfigurationBuilder Message<TMessage>()
    {
        subscription.RegisterMessageType<TMessage>();

        return this;
    }

    public ISubscriptionConfigurationBuilder Message(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        subscription.RegisterMessageType(messageType);

        return this;
    }

    public ISubscriptionConfigurationBuilder Messages(IEnumerable<Type> messageTypes)
    {
        ArgumentNullException.ThrowIfNull(messageTypes);

        foreach (var messageType in messageTypes)
        {
            subscription.RegisterMessageType(messageType);
        }

        return this;
    }

    public ISubscriptionConfigurationBuilder Handler(Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        subscription.HandlerDelegate = handler;

        return this;
    }

    public ISubscriptionConfigurationBuilder HandlerName(string handlerName)
    {
        subscription.HandlerName = handlerName;

        return this;
    }

    public ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition)
    {
        subscription.StartingPosition = startingPosition;

        return this;
    }

    public ISubscriptionConfigurationBuilder StreamScope(StreamScope streamScope)
    {
        subscription.StreamScope = streamScope;

        return this;
    }

    public ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount)
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException(
                "The max retry count must be greater than or equal to 0",
                nameof(maxRetryCount)
            );
        }

        subscription.MaxRetriesByExceptionType[typeof(Exception)] = maxRetryCount;

        return this;
    }

    public ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount) where TException : Exception
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException(
                "The max retry count must be greater than or equal to 0",
                nameof(maxRetryCount)
            );
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
