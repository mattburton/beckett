using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Commands;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<ICommandHandlerDispatcher>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );
    }
}
