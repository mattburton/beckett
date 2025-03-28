using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Streams;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IStreamReader, StreamReader>();
    }
}
