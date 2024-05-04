using System.Text.Json;
using Beckett.Storage.Postgres;
using Microsoft.AspNetCore.Http.Json;
using MinimalApi.TodoList;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var migrationsConnection = builder.Configuration.GetConnectionString("Migrations") ??
                       throw new Exception("Missing Migrations connection string");
var connectionString = builder.Configuration.GetConnectionString("TodoList") ??
                       throw new Exception("Missing TodoList connection string");

await Postgres.UpgradeSchema(migrationsConnection);
await EnsureAppDbUser(migrationsConnection);

builder.Services.AddNpgsqlDataSource(connectionString, options => options.AddBeckett());
await Postgres.UpgradeSchema(migrationsConnection);

builder.Services.AddBeckett(options =>
{
    options.UsePostgres(configuration =>
    {
        configuration.UseNotifications();
    });
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGroup("/todos").UseTodoListRoutes();

app.Run();
return;

async Task EnsureAppDbUser(string connection)
{
    var dataSource = new NpgsqlDataSourceBuilder(connection).Build();
    await using var createRole = dataSource.CreateCommand(
        "CREATE ROLE todo_app WITH LOGIN PASSWORD 'password';");
    try
    {
        await createRole.ExecuteNonQueryAsync();
    }
    catch (PostgresException e) when (e.SqlState == "42710")
    {
        // Role already exists
    }

    await using var assignRole = dataSource.CreateCommand(
        "GRANT beckett to todo_app;");
    {
        await assignRole.ExecuteNonQueryAsync();
    }
}

