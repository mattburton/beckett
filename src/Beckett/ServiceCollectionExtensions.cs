using Beckett.Configuration;
using Beckett.Dashboard;
using Beckett.Database;
using Beckett.Messages;
using Beckett.MessageStorage;
using Beckett.MessageStorage.Postgres;
using Beckett.OpenTelemetry;
using Beckett.Scheduling;
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
        Action<BeckettOptions>? configureOptions = null
    )
    {
        var options = builder.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                      new BeckettOptions();

        configureOptions?.Invoke(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddDashboardSupport();

        builder.Services.AddMessageSupport(options);

        builder.Services.AddMessageStorageSupport(options);

        builder.Services.AddOpenTelemetrySupport();

        builder.Services.AddPostgresMessageStorageSupport(options);

        builder.Services.AddPostgresSupport(options);

        builder.Services.AddRetrySupport(options);

        builder.Services.AddScheduledMessageSupport(options);

        builder.Services.AddSubscriptionSupport(options);

        var messageTypeProvider = new MessageTypeProvider();

        builder.Services.AddSingleton<IMessageTypeProvider>(messageTypeProvider);

        var messageTypeMap = new MessageTypeMap(options.Messages, messageTypeProvider);

        builder.Services.AddSingleton<IMessageTypeMap>(messageTypeMap);

        var subscriptionRegistry = new SubscriptionRegistry();

        builder.Services.AddSingleton<ISubscriptionRegistry>(subscriptionRegistry);

        var recurringMessageRegistry = new RecurringMessageRegistry();

        builder.Services.AddSingleton<IRecurringMessageRegistry>(recurringMessageRegistry);

        return new BeckettBuilder(
            builder.Configuration,
            builder.Environment,
            builder.Services,
            messageTypeMap,
            subscriptionRegistry,
            recurringMessageRegistry
        );
    }

    public static void AddBeckett(
        this IHostBuilder builder,
        Action<BeckettOptions>? configureOptions = null,
        Action<IBeckettBuilder>? buildBeckett = null
    )
    {
        builder.ConfigureServices(
            (context, services) =>
            {
                var configuration = context.Configuration;
                var environment = context.HostingEnvironment;

                var options = context.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                              new BeckettOptions();

                configureOptions?.Invoke(options);

                services.AddSingleton(options);

                services.AddDashboardSupport();

                services.AddMessageSupport(options);

                services.AddMessageStorageSupport(options);

                services.AddOpenTelemetrySupport();

                services.AddPostgresMessageStorageSupport(options);

                services.AddPostgresSupport(options);

                services.AddRetrySupport(options);

                services.AddScheduledMessageSupport(options);

                services.AddSubscriptionSupport(options);

                var messageTypeProvider = new MessageTypeProvider();

                services.AddSingleton<IMessageTypeProvider>(messageTypeProvider);

                var messageTypeMap = new MessageTypeMap(options.Messages, messageTypeProvider);

                services.AddSingleton<IMessageTypeMap>(messageTypeMap);

                var subscriptionRegistry = new SubscriptionRegistry();

                services.AddSingleton<ISubscriptionRegistry>(subscriptionRegistry);

                var recurringMessageRegistry = new RecurringMessageRegistry();

                services.AddSingleton<IRecurringMessageRegistry>(recurringMessageRegistry);

                var beckettBuilder = new BeckettBuilder(
                    configuration,
                    environment,
                    services,
                    messageTypeMap,
                    subscriptionRegistry,
                    recurringMessageRegistry
                );

                buildBeckett?.Invoke(beckettBuilder);
            }
        );
    }
}
