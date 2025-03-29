using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.MessageHandling;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();

        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<ICommandHandlerDispatcher>())
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IProcessorDispatcher>())
                .AsSelf()
                .WithTransientLifetime()
        );

        services.AddSingleton<IProcessorResultHandler, ProcessorResultHandler>();
    }
}
