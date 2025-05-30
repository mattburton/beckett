using Beckett.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TodoList.Infrastructure.Database;

public static class HostApplicationBuilderExtensions
{
    public static async Task AddTodoListDatabase(this IHostApplicationBuilder builder)
    {
        var migrationsConnectionString = builder.Configuration.GetConnectionString("Migrations") ??
                                         throw new Exception("Missing Migrations connection string");

        var applicationConnectionString = builder.Configuration.GetConnectionString("TodoList") ??
                                          throw new Exception("Missing TodoList connection string");

        await BeckettDatabase.Migrate(migrationsConnectionString);

        await TodoListApplicationUser.EnsureExists(migrationsConnectionString);

        builder.Services.AddNpgsqlDataSource(applicationConnectionString, options => options.AddBeckett());
    }
}
