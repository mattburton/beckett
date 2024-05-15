using Microsoft.Extensions.DependencyInjection;

namespace Beckett.OpenTelemetry;

public static class ServiceCollectionExtensions
{
    public static void AddOpenTelemetrySupport(this IServiceCollection services)
    {
        services.AddSingleton<IInstrumentation, Instrumentation>();
    }
}
