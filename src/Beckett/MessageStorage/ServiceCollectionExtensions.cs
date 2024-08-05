using Microsoft.Extensions.DependencyInjection;

namespace Beckett.MessageStorage;

public static class ServiceCollectionExtensions
{
    public static void AddMessageStorageSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton(typeof(IMessageStorage), options.MessageStorage.MessageStorageType);
    }
}
