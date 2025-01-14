using TaskHub.Infrastructure.DependencyInjection;

namespace TaskHub.Infrastructure.Commands;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services) => services.AddSingleton<ICommandExecutor, CommandExecutor>();
}
