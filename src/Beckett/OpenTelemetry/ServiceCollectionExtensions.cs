using Microsoft.Extensions.DependencyInjection;

namespace Beckett.OpenTelemetry;

public static class ServiceCollectionExtensions
{
    internal static void AddOpenTelemetrySupport(this IServiceCollection services) =>
        services.AddSingleton<IInstrumentation, Instrumentation>();
}
