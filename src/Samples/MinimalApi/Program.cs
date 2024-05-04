using System.Text.Json;
using Beckett.Storage.Postgres;
using Microsoft.AspNetCore.Http.Json;
using MinimalApi.TodoList;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TodoList") ??
                       throw new Exception("Missing TodoList connection string");

builder.Services.AddNpgsqlDataSource(connectionString, options => options.AddBeckett());

await Postgres.UpgradeSchema(connectionString);

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
