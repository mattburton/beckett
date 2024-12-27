using Beckett.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TaskHub.Infrastructure.Database;

public static class ServiceCollectionExtensions
{
    public static async Task AddTaskHubDatabase(this IHostApplicationBuilder builder)
    {
        var migrationsConnectionString = builder.Configuration.GetConnectionString("Migrations") ??
                                         throw new Exception("Missing Migrations connection string");

        var applicationConnectionString = builder.Configuration.GetConnectionString("TaskHub") ??
                                          throw new Exception("Missing TaskHub connection string");

        await BeckettDatabase.Migrate(migrationsConnectionString);

        await TaskHubDatabase.Migrate(migrationsConnectionString);

        builder.Services.AddNpgsqlDataSource(applicationConnectionString, options => options.AddBeckett());

        builder.Services.AddSingleton<IDatabase, Database>();
    }
}
