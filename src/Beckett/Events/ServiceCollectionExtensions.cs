using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public static class ServiceCollectionExtensions
{
    public static void AddEventSupport(this IServiceCollection services, BeckettOptions beckett)
    {
        EventTypeProvider.Initialize(beckett.Assemblies);
    }
}
