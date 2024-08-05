using Microsoft.Extensions.DependencyInjection;

namespace Beckett.MessageStorage.Postgres;

public static class ServiceCollectionExtensions
{
    public static void AddPostgresMessageStorageSupport(this IServiceCollection services, BeckettOptions options)
    {
        services.AddSingleton<IPostgresMessageDeserializer, PostgresMessageDeserializer>();
    }
}
