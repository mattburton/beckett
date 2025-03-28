using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Processors;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo<IProcessorDispatcher>())
                .AsSelf()
                .WithTransientLifetime()
        );

        services.AddSingleton<IResultProcessor, ResultProcessor>();
    }
}
