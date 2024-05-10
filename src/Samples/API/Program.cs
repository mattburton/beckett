using System.Text.Json;
using API;
using Beckett;
using Microsoft.AspNetCore.Http.Json;
using TodoList;
using TodoList.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett().UseTodoListModule();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<LogSwaggerLink>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGroup("/todos").UseTodoListRoutes();

app.Run();
