using System.Text.Json;
using API;
using Beckett;
using Beckett.OpenTelemetry;
using Microsoft.AspNetCore.Http.Json;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TodoList;
using TodoList.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<LogSwaggerLink>();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("todo-list-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddBeckett()
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
    );

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGroup("/todos").TodoListRoutes();

app.Run();
