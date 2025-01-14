namespace TaskHub.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        var serviceConfigurations = typeof(IConfigureServices).Assembly
            .GetTypes()
            .Where(x => x.IsAssignableTo(typeof(IConfigureServices)) && x is { IsAbstract: false, IsInterface: false })
            .Select(Activator.CreateInstance)
            .Cast<IConfigureServices>();

        foreach (var configuration in serviceConfigurations) configuration.Services(services);
    }
}
