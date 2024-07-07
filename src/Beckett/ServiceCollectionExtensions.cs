using Beckett.Configuration;
using Beckett.Dashboard;
using Beckett.Database;
using Beckett.Messages;
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

        builder.Services.AddDashboard();

        builder.Services.AddMessageSupport(options.Messages);

        builder.Services.AddOpenTelemetrySupport();

        builder.Services.AddPostgresSupport(options.Postgres);

        builder.Services.AddScheduledMessageSupport(options.Scheduling);

        builder.Services.AddSubscriptionSupport(options.Subscriptions);

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
        ).SubscriptionRetryModule(options.Subscriptions.Retries);
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

                services.AddDashboard();

                services.AddMessageSupport(options.Messages);

                services.AddOpenTelemetrySupport();

                services.AddPostgresSupport(options.Postgres);

                services.AddScheduledMessageSupport(options.Scheduling);

                services.AddSubscriptionSupport(options.Subscriptions);

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
                ).SubscriptionRetryModule(options.Subscriptions.Retries);

                buildBeckett?.Invoke(beckettBuilder);
            }
        );
    }
}
