using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using MinimalApi.TodoList;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBeckett(options =>
{
    options.UseAssemblies(typeof(Program).Assembly);

    options.Database.UseConnectionString("Server=localhost;Database=postgres;User Id=postgres;Password=password;");
    options.Database.UseSchema("event_store");
    options.Database.AutoMigrate();
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
