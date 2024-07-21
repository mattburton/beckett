using Beckett.Messages.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Messages;

public static class ServiceCollectionExtensions
{
    public static void AddMessageSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(options.Messages);

        services.AddSingleton<IMessageSerializer, MessageSerializer>();

        services.AddSingleton<IMessageStore, MessageStore>();

        services.AddHostedService<MessageTypeMappingDiagnosticService>();
    }
}
