using System.Text.RegularExpressions;
using Beckett.Messages;
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

    void Map<TMessage>(string name);

    ISubscriptionBuilder AddSubscription(string name);
}

public interface ISubscriptionBuilder
{
    ICategorySubscriptionBuilder Category(string category);
}

public interface ISubscriptionConfigurationBuilder
{
    ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition);
    ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount);
}

public interface ICategorySubscriptionBuilder
{
    ICategoryMessageSubscriptionBuilder<TMessage> Message<TMessage>();

    ISubscriptionConfigurationBuilder Handler<THandler>(MessageContextHandler<THandler> handler);
    ISubscriptionConfigurationBuilder Handler(StaticMessageContextHandler handler);
}

public interface ICategoryMessageSubscriptionBuilder<out TMessage>
{
    ICategoryMessageSubscriptionBuilder<T> Message<T>();

    ISubscriptionConfigurationBuilder Handler<THandler>(TypedMessageHandler<THandler, TMessage> handler);
    ISubscriptionConfigurationBuilder Handler<THandler>(TypedMessageAndContextHandler<THandler, TMessage> handler);
    ISubscriptionConfigurationBuilder Handler<THandler>(MessageContextHandler<THandler> handler);
    ISubscriptionConfigurationBuilder Handler<THandler>(StaticTypedMessageHandler<TMessage> handler);
    ISubscriptionConfigurationBuilder Handler<THandler>(StaticTypedMessageAndContextHandler<TMessage> handler);
    ISubscriptionConfigurationBuilder Handler(StaticMessageContextHandler handler);
}

public class BeckettBuilder(
    IConfiguration configuration,
    IHostEnvironment environment,
    IServiceCollection services,
    IMessageTypeMap messageTypeMap,
    ISubscriptionRegistry subscriptionRegistry
) : IBeckettBuilder
{
    public IConfiguration Configuration { get; } = configuration;
    public IHostEnvironment Environment { get; } = environment;
    public IServiceCollection Services { get; } = services;

    public void Map<TMessage>(string name) => messageTypeMap.Map<TMessage>(name);

    public ISubscriptionBuilder AddSubscription(string name)
    {
        if (!subscriptionRegistry.TryAdd(name, out var subscription))
        {
            throw new InvalidOperationException($"There is already a subscription with the name {name}");
        }

        return new SubscriptionBuilder(subscription);
    }

    private class SubscriptionBuilder(Subscription subscription) : ISubscriptionBuilder
    {
        public ICategorySubscriptionBuilder Category(string category)
        {
            subscription.Category = category;
            subscription.Pattern = new Regex($"({Regex.Escape(category)})(?=-*)", RegexOptions.Compiled);

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

        public ISubscriptionConfigurationBuilder Handler<THandler>(MessageContextHandler<THandler> handler)
        {
            subscription.Type = typeof(THandler);
            subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);
            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler(StaticMessageContextHandler handler)
        {
            subscription.Type = handler.GetHandlerType();
            subscription.StaticMethod = (c, t) => handler((IMessageContext)c, t);
            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }
    }

    private class CategoryMessageSubscriptionBuilder<T>(Subscription subscription) : ICategoryMessageSubscriptionBuilder<T>
    {
        public ICategoryMessageSubscriptionBuilder<TMessage> Message<TMessage>()
        {
            subscription.MessageTypes.Add(typeof(TMessage));

            return new CategoryMessageSubscriptionBuilder<TMessage>(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(TypedMessageHandler<THandler, T> handler)
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            subscription.Type = typeof(THandler);
            subscription.InstanceMethod = (h, m, t) => handler((THandler)h, (T)m, t);

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(TypedMessageAndContextHandler<THandler, T> handler)
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            subscription.Type = typeof(THandler);

            subscription.InstanceMethod = (h, c, t) => handler(
                (THandler)h,
                (T)((IMessageContext)c).Message,
                (IMessageContext)c,
                t
            );

            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(MessageContextHandler<THandler> handler)
        {
            subscription.Type = typeof(THandler);
            subscription.InstanceMethod = (h, c, t) => handler((THandler)h, (IMessageContext)c, t);

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(StaticTypedMessageHandler<T> handler)
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            subscription.Type = handler.GetHandlerType();
            subscription.StaticMethod = (m, t) => handler((T)m, t);
            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler<THandler>(StaticTypedMessageAndContextHandler<T> handler)
        {
            subscription.EnsureOnlyHandlerMessageTypeIsMapped<T>();

            subscription.Type = handler.GetHandlerType();
            subscription.StaticMethod = (c, t) => handler((T)((IMessageContext)c).Message, (IMessageContext)c, t);
            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }

        public ISubscriptionConfigurationBuilder Handler(StaticMessageContextHandler handler)
        {
            subscription.Type = handler.GetHandlerType();
            subscription.StaticMethod = (c, t) => handler((IMessageContext)c, t);
            subscription.AcceptsMessageContext = true;

            return new SubscriptionConfigurationBuilder(subscription);
        }
    }

    private class SubscriptionConfigurationBuilder(Subscription subscription) : ISubscriptionConfigurationBuilder
    {
        public ISubscriptionConfigurationBuilder StartingPosition(StartingPosition startingPosition)
        {
            subscription.StartingPosition = startingPosition;

            return this;
        }

        public ISubscriptionConfigurationBuilder MaxRetryCount(int maxRetryCount)
        {
            subscription.MaxRetryCount = maxRetryCount;

            return this;
        }
    }
}

internal static class HandlerDelegateExtensions
{
    public static Type GetHandlerType(this Delegate handler)
    {
        var handlerType = handler.Method.DeclaringType ??
                          throw new InvalidOperationException("Could not determine the handler type");

        if (!handler.Method.IsStatic)
        {
            throw new InvalidOperationException($"The method being called on {handlerType} must be static");
        }

        return handlerType;
    }
}
