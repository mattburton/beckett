using System.Text.Json;
using Beckett.Storage.Postgres;
using Microsoft.AspNetCore.Http.Json;
using MinimalApi.TodoList;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("TodoList") ?? throw new Exception("Missing TodoList connection string"),
    options => options.AddBeckett()
);

builder.Services.AddBeckett(options =>
{
    options.UsePostgres(configuration =>
    {
        configuration.UseNotifications();
        configuration.AutoMigrate();
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
