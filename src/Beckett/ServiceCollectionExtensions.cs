using Beckett.Configuration;
using Beckett.Dashboard;
using Beckett.Database;
using Beckett.MessageStorage;
using Beckett.OpenTelemetry;
using Beckett.Scheduling;
using Beckett.Storage;
using Beckett.Subscriptions;
using Beckett.Subscriptions.Retries;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Beckett support to the host. Without configuring any options Beckett will use the registered
    /// <see cref="Npgsql.NpgsqlDataSource"/> and register those dependencies required to run in a client configuration
    /// - <see cref="IMessageStore"/>, and so on. Subscription support must be enabled explicitly by supplying an action
    /// to configure Beckett options.
    /// </summary>
    /// <param name="services">The .NET host service collection</param>
    /// <param name="configure">Action to configure Beckett options</param>
    /// <returns>Beckett builder that can be used to configure the application further</returns>
    public static IBeckettBuilder AddBeckett(
        this IServiceCollection services,
        Action<BeckettOptions>? configure = null
    )
    {
        var options = new BeckettOptions();

        configure?.Invoke(options);

        services.AddSingleton(options);

        services.AddSingleton<IMessageStore, MessageStore>();

        services.AddDashboardSupport();

        services.AddMessageStorageSupport(options);

        services.AddOpenTelemetrySupport();

        services.AddPostgresSupport(options);

        services.AddRetrySupport(options);

        services.AddScheduledMessageSupport(options);

        services.AddSubscriptionSupport(options);

        return new BeckettBuilder(services);
    }
}
