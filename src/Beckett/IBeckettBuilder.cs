using Beckett.Events;
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

    void MapEvent<TEvent>(string name);

    void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandler<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    );

    void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandlerWithContext<THandler, TEvent> handler,
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
    IEventTypeMap eventTypeMap,
    ISubscriptionRegistry subscriptionRegistry
) : IBeckettBuilder
{
    public IConfiguration Configuration { get; } = configuration;
    public IHostEnvironment Environment { get; } = environment;
    public IServiceCollection Services { get; } = services;

    public void MapEvent<TEvent>(string name) => eventTypeMap.Map<TEvent>(name);

    public void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandler<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);

    public void AddSubscription<THandler, TEvent>(
        string name,
        SubscriptionHandlerWithContext<THandler, TEvent> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);

    public void AddSubscription<THandler>(
        string name,
        SubscriptionHandler<THandler> handler,
        Action<Subscription>? configure = null
    ) => subscriptionRegistry.AddSubscription(name, handler, configure);
}
