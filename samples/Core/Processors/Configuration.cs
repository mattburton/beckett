using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Processors;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IProcessor>())
                .AsSelf()
                .WithTransientLifetime()
        );

        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IBatchProcessor>())
                .AsSelf()
                .WithTransientLifetime()
        );

        services.AddSingleton<IBatchMessageStorage, BatchMessageStorage>();

        services.AddSingleton<IBatchMessageScheduler, BatchMessageScheduler>();

        services.AddSingleton<IProcessorResultHandler, ProcessorResultHandler>();
    }
}
