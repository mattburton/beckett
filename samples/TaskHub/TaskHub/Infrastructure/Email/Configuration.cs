using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace TaskHub.Infrastructure.Email;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IEmailService, LoggingEmailService>();
    }
}
