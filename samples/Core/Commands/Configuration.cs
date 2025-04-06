using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Commands;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
    }
}
