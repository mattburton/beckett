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
        Action<BeckettOptions>? configure = null
    )
    {
        var options = builder.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                      new BeckettOptions();

        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

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
        ).SubscriptionRetryModule();
    }

    public static IBeckettBuilder AddBeckett(this IHostBuilder builder, Action<BeckettOptions>? configure = null)
    {
        IConfiguration configuration = null!;
        IHostEnvironment environment = null!;
        IServiceCollection serviceCollection = null!;
        BeckettOptions options;
        IMessageTypeMap messageTypeMap = null!;
        ISubscriptionRegistry subscriptionRegistry = null!;
        IRecurringMessageRegistry recurringMessageRegistry = null!;

        builder.ConfigureServices(
            (context, services) =>
            {
                configuration = context.Configuration;
                environment = context.HostingEnvironment;
                serviceCollection = services;

                options = context.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                          new BeckettOptions();

                configure?.Invoke(options);

                services.AddSingleton(options);

                services.AddMessageSupport(options.Messages);

                services.AddOpenTelemetrySupport();

                services.AddPostgresSupport(options.Postgres);

                services.AddScheduledMessageSupport(options.Scheduling);

                services.AddSubscriptionSupport(options.Subscriptions);

                var messageTypeProvider = new MessageTypeProvider();

                services.AddSingleton<IMessageTypeProvider>(messageTypeProvider);

                messageTypeMap = new MessageTypeMap(options.Messages, messageTypeProvider);

                services.AddSingleton(messageTypeMap);

                subscriptionRegistry = new SubscriptionRegistry();

                services.AddSingleton(subscriptionRegistry);

                recurringMessageRegistry = new RecurringMessageRegistry();

                services.AddSingleton(recurringMessageRegistry);
            }
        );

        return new BeckettBuilder(
            configuration,
            environment,
            serviceCollection,
            messageTypeMap,
            subscriptionRegistry,
            recurringMessageRegistry
        ).SubscriptionRetryModule();
    }
}
