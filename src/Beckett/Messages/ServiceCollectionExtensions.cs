using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Messages;

public static class ServiceCollectionExtensions
{
    public static void AddMessageSupport(this IServiceCollection services)
    {
        services.AddSingleton<IMessageStore, MessageStore>();
    }
}
