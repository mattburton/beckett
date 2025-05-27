using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class ServiceCollectionExtensions
{
    internal static void AddRetrySupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Subscriptions.Retries);
    }
}
