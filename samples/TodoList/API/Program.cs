using System.Text.Json;
using API.TodoLists;
using Beckett.Dashboard;
using Beckett.OpenTelemetry;
using Microsoft.AspNetCore.Http.Json;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TodoList;
using TodoList.Infrastructure.Database;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    await builder.AddTodoListDatabase();

    builder.Services.AddBeckett().WithTodoListMessageTypes();

    builder.Services.AddBeckettDashboard();

    builder.Services.Configure<JsonOptions>(
        options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        }
    );

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("todo-list-api"))
        .WithTracing(
            tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddBeckett()
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
        );

    var app = builder.Build();

    app.MapBeckettDashboard("/beckett");

    app.MapTodoListRoutes();

    app.Map("/", () => "Hello World!");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception during startup");
}
finally
{
    Log.CloseAndFlush();
}
