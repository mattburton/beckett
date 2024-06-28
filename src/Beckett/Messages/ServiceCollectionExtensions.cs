using Beckett.Messages.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Messages;

public static class ServiceCollectionExtensions
{
    public static void AddMessageSupport(this IServiceCollection services, MessageOptions options)
    {
        services.AddSingleton(options);

        services.AddSingleton<IMessageSerializer, MessageSerializer>();

        services.AddSingleton<IMessageStore, MessageStore>();

        services.AddTransient<IMessageSession, MessageSession>();

        services.AddHostedService<MessageTypeMappingDiagnosticService>();
    }
}
