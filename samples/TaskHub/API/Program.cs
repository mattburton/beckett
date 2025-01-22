using System.Text.Json;
using Beckett;
using Beckett.Dashboard;
using Beckett.OpenTelemetry;
using Microsoft.AspNetCore.Http.Json;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TaskHub.Infrastructure.Database;
using TaskHub.Infrastructure.DependencyInjection;
using TaskHub.Infrastructure.Modules;
using TaskHub.Infrastructure.Routing;
using TaskHub.TaskLists;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    await builder.AddTaskHubDatabase();

    builder.Services.ConfigureServices();

    builder.Services.AddBeckett().WithMessageTypesFrom(typeof(TaskListModule).Assembly);

    builder.Services.AddBeckettDashboard();

    builder.Services.Configure<JsonOptions>(
        options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        }
    );

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("taskhub-api"))
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

    app.MapGroup("/taskhub").MapRoutes();

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
