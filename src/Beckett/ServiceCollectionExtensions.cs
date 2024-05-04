using Beckett.Database;
using Beckett.Events;
using Beckett.ScheduledEvents;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett;

public static class ServiceCollectionExtensions
{
    public static IBeckettBuilder AddBeckett(
        this IHostApplicationBuilder builder,
        Action<BeckettOptions>? configure = null
    )
    {
        var options = builder.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                      new BeckettOptions();

        configure?.Invoke(options);

        builder.Services.AddPostgresSupport(options.Postgres);

        builder.Services.AddEventSupport(options.Events);

        builder.Services.AddScheduledEventSupport(options.ScheduledEvents);

        builder.Services.AddSubscriptionSupport(options.Subscriptions);

        builder.Services.AddSingleton<IEventStore, EventStore>();

        var eventTypeProvider = new EventTypeProvider();

        builder.Services.AddSingleton<IEventTypeProvider>(eventTypeProvider);

        var eventTypeMap = new EventTypeMap(options.Events, eventTypeProvider);

        builder.Services.AddSingleton<IEventTypeMap>(eventTypeMap);

        var subscriptionRegistry = new SubscriptionRegistry(eventTypeMap);

        builder.Services.AddSingleton<ISubscriptionRegistry>(subscriptionRegistry);

        return new BeckettBuilder(
            builder.Configuration,
            builder.Environment,
            builder.Services,
            eventTypeMap,
            subscriptionRegistry
        ).UseSubscriptionRetries();
    }

    public static IBeckettBuilder AddBeckett(this IHostBuilder builder, Action<BeckettOptions>? configure = null)
    {
        IConfiguration configuration = null!;
        IHostEnvironment environment = null!;
        IServiceCollection serviceCollection = null!;
        BeckettOptions options;
        IEventTypeMap eventTypeMap = null!;
        ISubscriptionRegistry subscriptionRegistry = null!;

        builder.ConfigureServices((context, services) =>
        {
            configuration = context.Configuration;
            environment = context.HostingEnvironment;
            serviceCollection = services;

            options = context.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                      new BeckettOptions();

            configure?.Invoke(options);

            services.AddSingleton(options);

            services.AddPostgresSupport(options.Postgres);

            services.AddEventSupport(options.Events);

            services.AddScheduledEventSupport(options.ScheduledEvents);

            services.AddSubscriptionSupport(options.Subscriptions);

            services.AddSingleton<IEventStore, EventStore>();

            var eventTypeProvider = new EventTypeProvider();

            services.AddSingleton<IEventTypeProvider>(eventTypeProvider);

            eventTypeMap = new EventTypeMap(options.Events, eventTypeProvider);

            services.AddSingleton(eventTypeMap);

            subscriptionRegistry = new SubscriptionRegistry(eventTypeMap);

            services.AddSingleton(subscriptionRegistry);
        });

        return new BeckettBuilder(
            configuration,
            environment,
            serviceCollection,
            eventTypeMap,
            subscriptionRegistry
        ).UseSubscriptionRetries();
    }
}
