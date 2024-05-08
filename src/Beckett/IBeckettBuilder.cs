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

    void MapMessage<TMessage>(string name);

    void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandler<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandlerWithContext<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler>(
        string name,
        SubscriptionHandler<THandler> handler,
        Action<Subscription>? configure = null
    );
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

    public void MapMessage<TMessage>(string name) => messageTypeMap.Map<TMessage>(name);

    public void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandler<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);

    public void AddSubscription<THandler, TMessage>(
        string name,
        SubscriptionHandlerWithContext<THandler, TMessage> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);

    public void AddSubscription<THandler>(
        string name,
        SubscriptionHandler<THandler> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);
}
