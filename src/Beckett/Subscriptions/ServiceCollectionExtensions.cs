using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Subscriptions;

public static class ServiceCollectionExtensions
{
    public static void AddSubscriptionSupport(this IServiceCollection services, BeckettOptions beckett)
    {
        if (!beckett.Subscriptions.Enabled)
        {
            return;
        }

        services.AddSingleton<ISubscriptionProcessor, SubscriptionProcessor>();
    }
}
