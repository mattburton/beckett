using TaskHub.Infrastructure.DependencyInjection;

namespace TaskHub.Infrastructure.Notifications;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<INotificationPublisher, NotificationPublisher>();
    }
}
