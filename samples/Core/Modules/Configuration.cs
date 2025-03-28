using Core.Contracts;
using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Modules;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IModule>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
        );
    }
}
