using Beckett.Database;
using Beckett.Messages;
using Beckett.Messages.Scheduling;
using Beckett.OpenTelemetry;
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

        builder.Services.AddMessageSupport(options.Messages);

        builder.Services.AddOpenTelemetrySupport();

        builder.Services.AddPostgresSupport(options.Postgres);

        builder.Services.AddScheduledMessageSupport(options.ScheduledMessages);

        builder.Services.AddSubscriptionSupport(options.Subscriptions);

        builder.Services.AddSingleton<IMessageStore, MessageStore>();

        var messageTypeProvider = new MessageTypeProvider();

        builder.Services.AddSingleton<IMessageTypeProvider>(messageTypeProvider);

        var messageTypeMap = new MessageTypeMap(options.Messages, messageTypeProvider);

        builder.Services.AddSingleton<IMessageTypeMap>(messageTypeMap);

        var subscriptionRegistry = new SubscriptionRegistry();

        builder.Services.AddSingleton<ISubscriptionRegistry>(subscriptionRegistry);

        return new BeckettBuilder(
            builder.Configuration,
            builder.Environment,
            builder.Services,
            messageTypeMap,
            subscriptionRegistry
        ).UseSubscriptionRetries();
    }

    public static IBeckettBuilder AddBeckett(this IHostBuilder builder, Action<BeckettOptions>? configure = null)
    {
        IConfiguration configuration = null!;
        IHostEnvironment environment = null!;
        IServiceCollection serviceCollection = null!;
        BeckettOptions options;
        IMessageTypeMap messageTypeMap = null!;
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

            services.AddMessageSupport(options.Messages);

            services.AddOpenTelemetrySupport();

            services.AddPostgresSupport(options.Postgres);

            services.AddScheduledMessageSupport(options.ScheduledMessages);

            services.AddSubscriptionSupport(options.Subscriptions);

            services.AddSingleton<IMessageStore, MessageStore>();

            var messageTypeProvider = new MessageTypeProvider();

            services.AddSingleton<IMessageTypeProvider>(messageTypeProvider);

            messageTypeMap = new MessageTypeMap(options.Messages, messageTypeProvider);

            services.AddSingleton(messageTypeMap);

            subscriptionRegistry = new SubscriptionRegistry();

            services.AddSingleton(subscriptionRegistry);
        });

        return new BeckettBuilder(
            configuration,
            environment,
            serviceCollection,
            messageTypeMap,
            subscriptionRegistry
        ).UseSubscriptionRetries();
    }
}
