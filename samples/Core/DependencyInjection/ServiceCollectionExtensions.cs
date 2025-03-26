using System.Reflection;
using Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        var serviceConfigurations = Assembly.GetEntryAssembly()!
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .SelectMany(x => x.GetLoadableTypes())
            .Where(x => x.IsAssignableTo(typeof(IConfigureServices)) && x is { IsAbstract: false, IsInterface: false })
            .Select(Activator.CreateInstance)
            .Cast<IConfigureServices>();

        foreach (var configuration in serviceConfigurations)
        {
            configuration.Services(services);
        }
    }
}
