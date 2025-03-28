using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Batching;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IBatchMessageStorage, BatchMessageStorage>();

        services.AddSingleton<IBatchMessageScheduler, BatchMessageScheduler>();
    }
}
