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

        builder.Services.AddMessageSupport(options);

        builder.Services.AddOpenTelemetrySupport();

        builder.Services.AddPostgresSupport(options);

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

        var beckettBuilder = new BeckettBuilder(
            builder.Configuration,
            builder.Environment,
            builder.Services,
            messageTypeMap,
            subscriptionRegistry,
            recurringMessageRegistry
        ).RetryClientSupport();

        return !options.Subscriptions.Enabled
            ? beckettBuilder
            : beckettBuilder.RetryServerSupport(options.Subscriptions.Retries);
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

                services.AddMessageSupport(options);

                services.AddOpenTelemetrySupport();

                services.AddPostgresSupport(options);

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
                ).RetryClientSupport();

                if (options.Subscriptions.Enabled)
                {
                    beckettBuilder.RetryServerSupport(options.Subscriptions.Retries);
                }

                buildBeckett?.Invoke(beckettBuilder);
            }
        );
    }
}
