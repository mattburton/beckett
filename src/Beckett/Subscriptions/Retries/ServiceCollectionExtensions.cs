using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions.Retries;

public static class ServiceCollectionExtensions
{
    public static void AddRetrySupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Subscriptions.Retries);

        services.AddSingleton<IRetryClient, RetryClient>();
    }
}
