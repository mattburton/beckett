using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Commands;

public static class ServiceCollectionExtensions
{
    public static void AddCommandSupport(this IServiceCollection services)
    {
        services.AddSingleton<ICommandExecutor, CommandExecutor>();
    }
}
