using Beckett.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TodoList.Infrastructure.Database;

public static class ServiceCollectionExtensions
{
    public static async Task AddTodoListDatabase(this IHostApplicationBuilder builder)
    {
        var migrationsConnectionString = builder.Configuration.GetConnectionString("Migrations") ??
                                         throw new Exception("Missing Migrations connection string");

        var applicationConnectionString = builder.Configuration.GetConnectionString("TodoList") ??
                                          throw new Exception("Missing TodoList connection string");

        await Postgres.UpgradeSchema(migrationsConnectionString);

        await TodoListApplicationUser.EnsureExists(migrationsConnectionString);

        builder.Services.AddNpgsqlDataSource(applicationConnectionString, options => options.AddBeckett());
    }
}
