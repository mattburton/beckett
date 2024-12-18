using Beckett.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Taskmaster.Infrastructure.Database;

public static class ServiceCollectionExtensions
{
    public static async Task AddTaskmasterDatabase(this IHostApplicationBuilder builder)
    {
        var migrationsConnectionString = builder.Configuration.GetConnectionString("Migrations") ??
                                         throw new Exception("Missing Migrations connection string");

        var applicationConnectionString = builder.Configuration.GetConnectionString("Taskmaster") ??
                                          throw new Exception("Missing Taskmaster connection string");

        await BeckettDatabase.Migrate(migrationsConnectionString);

        await TaskmasterApplicationUser.EnsureExists(migrationsConnectionString);

        builder.Services.AddNpgsqlDataSource(applicationConnectionString, options => options.AddBeckett());
    }
}
