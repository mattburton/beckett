using Beckett.Configuration;
using Beckett.Dashboard;
using Beckett.Database;
using Beckett.MessageStorage;
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
    /// <summary>
    /// Add Beckett support to the host. Without configuring any options Beckett will use the registered
    /// <see cref="Npgsql.NpgsqlDataSource"/> and register those dependencies required to run in a client configuration
    /// - <see cref="IMessageStore"/>, and so on. Subscription support must be enabled explicitly by supplying an action
    /// to configure Beckett options.
    /// </summary>
    /// <param name="builder">The .NET host application builder</param>
    /// <param name="configureOptions">Action to configure Beckett options</param>
    /// <returns>Beckett builder that can be used to configure individual application modules</returns>
    public static IBeckettBuilder AddBeckett(
        this IHostApplicationBuilder builder,
        Action<BeckettOptions>? configureOptions = null
    )
    {
        var options = builder.Configuration.GetSection(BeckettOptions.SectionName).Get<BeckettOptions>() ??
                      new BeckettOptions();

        configureOptions?.Invoke(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddSingleton<IMessageStore, MessageStore>();

        builder.Services.AddDashboardSupport();

        builder.Services.AddMessageStorageSupport(options);

        builder.Services.AddOpenTelemetrySupport();

        builder.Services.AddPostgresSupport(options);

        builder.Services.AddRetrySupport(options);

        builder.Services.AddScheduledMessageSupport(options);

        builder.Services.AddSubscriptionSupport(options);

        return new BeckettBuilder(
            builder.Configuration,
            builder.Environment,
            builder.Services
        );
    }

    /// <summary>
    /// Add Beckett support to the host. Without configuring any options Beckett will use the registered
    /// <see cref="Npgsql.NpgsqlDataSource"/> and register those dependencies required to run in a client configuration
    /// - <see cref="IMessageStore"/>, and so on. Subscription support must be enabled explicitly by supplying an action
    /// to configure Beckett options.
    /// </summary>
    /// <param name="builder">The .NET host builder</param>
    /// <param name="configureOptions">Configure Beckett options</param>
    /// <param name="buildBeckett">Apply Beckett builders to configure individual application modules</param>
    /// <returns></returns>
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

                services.AddSingleton<IMessageStore, MessageStore>();

                services.AddDashboardSupport();

                services.AddMessageStorageSupport(options);

                services.AddOpenTelemetrySupport();

                services.AddPostgresSupport(options);

                services.AddRetrySupport(options);

                services.AddScheduledMessageSupport(options);

                services.AddSubscriptionSupport(options);

                var beckettBuilder = new BeckettBuilder(
                    configuration,
                    environment,
                    services
                );

                buildBeckett?.Invoke(beckettBuilder);
            }
        );
    }
}
