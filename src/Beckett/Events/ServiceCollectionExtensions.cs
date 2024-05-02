using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

internal static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services, BeckettOptions options)
    {
        EventTypeProvider.Initialize(options.Assemblies);
    }
}
