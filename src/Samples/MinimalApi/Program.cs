using System.Text.Json;
using Beckett.Database;
using Microsoft.AspNetCore.Http.Json;
using MinimalApi.Infrastructure.Database;
using MinimalApi.Infrastructure.Swagger;
using MinimalApi.TodoList;

var builder = WebApplication.CreateBuilder(args);

var migrationsConnectionString = builder.Configuration.GetConnectionString("Migrations") ??
                                 throw new Exception("Missing Migrations connection string");

var applicationConnectionString = builder.Configuration.GetConnectionString("TodoList") ??
                                  throw new Exception("Missing TodoList connection string");

await Postgres.UpgradeSchema(migrationsConnectionString);

await TodoListApplicationUser.EnsureExists(migrationsConnectionString);

builder.Services.AddNpgsqlDataSource(applicationConnectionString, options => options.AddBeckett());

builder.AddBeckett().UseTodoListModule();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<LogLinkToApiDocumentationAtStartup>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGroup("/todos").UseTodoListRoutes();

app.Run();
