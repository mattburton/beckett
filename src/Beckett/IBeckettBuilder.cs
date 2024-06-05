using Beckett.Messages;
using Beckett.Scheduling;
using Beckett.Subscriptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett;

public interface IBeckettBuilder
{
    IConfiguration Configuration { get; }
    IHostEnvironment Environment { get; }
    IServiceCollection Services { get; }

    ISubscriptionBuilder AddSubscription(string name);

    IBeckettBuilder Build(Action<IBeckettBuilder> configure);

    void Map<TMessage>(string name);

    void ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        Dictionary<string, object>? metadata = null
    ) where TMessage : notnull;
}

public interface ISubscriptionBuilder
{
    ICategorySubscriptionBuilder Category(string category);
}

public interface ICategorySubscriptionBuilder
{
    ICategoryMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler(
        Func<IMessageContext, CancellationToken, Task> handler,
        string handlerName
    );
}

public interface ICategoryMessageSubscriptionBuilder<out TMessage>
{
    ICategoryMessageSubscriptionBuilder<T> Message<T>();

    ISubscriptionConfigurationBuilder Handler<THandler>(Func<THandler, TMessage, CancellationToken, Task> handler);

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, TMessage, IMessageContext, CancellationToken, Task> handler
    );

    ISubscriptionConfigurationBuilder Handler<THandler>(
        Func<THandler, IMessageContext, CancellationToken, Task> handler
    );
}

public interface ISubscriptionConfigurationBuilder
{
    ISubscriptionConfigurationBuilder HandlerName(string name);
    ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition);
    ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount);
    ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount) where TException : Exception;
}

public class BeckettBuilder(
    IConfiguration configuration,
    IHostEnvironment environment,
    IServiceCollection services,
    IMessageTypeMap messageTypeMap,
    ISubscriptionRegistry subscriptionRegistry,
    IRecurringMessageRegistry recurringMessageRegistry
) : IBeckettBuilder
{
    public IConfiguration Configuration { get; } = configuration;
    public IHostEnvironment Environment { get; } = environment;
    public IServiceCollection Services { get; } = services;

    public ISubscriptionBuilder AddSubscription(string name)
    {
        if (!subscriptionRegistry.TryAdd(name, out var subscription))
        {
            throw new InvalidOperationException($"There is already a subscription with the name {name}");
        }

        return new SubscriptionBuilder(subscription);
    }

    public IBeckettBuilder Build(Action<IBeckettBuilder> build)
    {
        build(this);

        return this;
    }

    public void Map<TMessage>(string name) => messageTypeMap.Map<TMessage>(name);

    public void ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        Dictionary<string, object>? metadata = null
    ) where TMessage : notnull
    {
        if (!recurringMessageRegistry.TryAdd(name, out var recurringMessage))
        {
            throw new InvalidOperationException($"There is already a recurring message with the name {name}");
        }

        recurringMessage.CronExpression = cronExpression;
        recurringMessage.StreamName = streamName;
        recurringMessage.Message = message;
        recurringMessage.Metadata = metadata ?? new Dictionary<string, object>();
    }

    private class SubscriptionBuilder(Subscription subscription) : ISubscriptionBuilder
    {
        public ICategorySubscriptionBuilder Category(string category)
        {
            subscription.Category = category;

            return new CategorySubscriptionBuilder(subscription);
        }
    }

    private class CategorySubscriptionBuilder(Subscription subscription) : ICategorySubscriptionBuilder
    {
        public ICategoryMessageSubscriptionBuilder<TMessage> Message<TMessage>()
        {
            subscription.MessageTypes.Add(typeof(TMessage));

            return new CategoryMessageSubscriptionBuilder<TMessage>(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(
            Func<THandler, IMessageContext, CancellationToken, Task> handler
        )
        {
            var handlerType = typeof(THandler);

            subscription.HandlerType = handlerType;
            subscription.HandlerName = handlerType.FullName;
            subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler(
            Func<IMessageContext, CancellationToken, Task> handler,
            string handlerName
        )
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

            subscription.HandlerName = handlerName;
            subscription.StaticMethod = (c, t) => handler((IMessageContext)c, t);

            return new SubscriptionConfigurationBuilder(subscription);
        }
    }

    private class CategoryMessageSubscriptionBuilder<T>(Subscription subscription)
        : ICategoryMessageSubscriptionBuilder<T>
    {
        public ICategoryMessageSubscriptionBuilder<TMessage> Message<TMessage>()
        {
            subscription.MessageTypes.Add(typeof(TMessage));

            return new CategoryMessageSubscriptionBuilder<TMessage>(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(
            Func<THandler, T, CancellationToken, Task> handler
        )
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            var handlerType = typeof(THandler);

            subscription.HandlerType = handlerType;
            subscription.HandlerName = handlerType.FullName;
            subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (T)((IMessageContext)c).Message, t);

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(
            Func<THandler, T, IMessageContext, CancellationToken, Task> handler
        )
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            var handlerType = typeof(THandler);

            subscription.HandlerType = handlerType;
            subscription.HandlerName = handlerType.FullName;
            subscription.InstanceMethod = (h, c, t) => handler(
                (THandler)h,
                (T)((IMessageContext)c).Message,
                (IMessageContext)c,
                t
            );

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(
            Func<THandler, IMessageContext, CancellationToken, Task> handler
        )
        {
            var handlerType = typeof(THandler);

            subscription.HandlerType = handlerType;
            subscription.HandlerName = handlerType.FullName;
            subscription.InstanceMethod = (h, c, t) => handler(
                (THandler)h,
                (IMessageContext)c,
                t
            );

            return new SubscriptionConfigurationBuilder(subscription);
        }
    }

    private class SubscriptionConfigurationBuilder(Subscription subscription) : ISubscriptionConfigurationBuilder
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
                throw new InvalidOperationException($"{nameof(MaxRetryCount)} must be greater than or equal to 0");
            }

            subscription.MaxRetriesByExceptionType[typeof(Exception)] = maxRetryCount;

            return this;
        }

        public ISubscriptionConfigurationBuilder MaxRetryCount<TException>(int maxRetryCount)
            where TException : Exception
        {
            if (maxRetryCount < 0)
            {
                throw new InvalidOperationException($"{nameof(MaxRetryCount)} must be greater than or equal to 0");
            }

            subscription.MaxRetriesByExceptionType[typeof(TException)] = maxRetryCount;

            return this;
        }
    }
}
