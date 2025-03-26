using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Notifications;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();
    }
}
