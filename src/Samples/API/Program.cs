using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using API.TodoList;
using Beckett;
using Beckett.Dashboard;
using Beckett.Messages;
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

    var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerOptions.Default)
    {
        TypeInfoResolver = TodoListJsonSerializerContext.Default
    };

    MessageSerializer.Configure(jsonSerializerOptions);

    builder.AddBeckett().WithClientConfigurationFrom(typeof(TodoList.TodoList).Assembly);

    builder.Services.Configure<JsonOptions>(
        options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
                TodoListApiJsonSerializerContext.Default,
                TodoListJsonSerializerContext.Default
            );
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

    app.MapGroup("/todos").TodoListRoutes();

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
