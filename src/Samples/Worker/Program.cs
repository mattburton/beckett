using Beckett;
using Beckett.OpenTelemetry;
using Npgsql;
using OpenTelemetry.Metrics;
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
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    await builder.AddTodoListDatabase();

    builder.AddBeckett(
        options =>
        {
            options.WithSubscriptionGroup("TodoList");

            options.Subscriptions.MaxRetryCount<RetryableException>(10);

            options.Subscriptions.MaxRetryCount<TerminalException>(0);
        }
    ).WithServerConfigurationFrom(typeof(TodoList.TodoList).Assembly);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("todo-list-worker"))
        .WithMetrics(
            metrics => metrics
                .AddBeckett()
                .AddConsoleExporter()
        )
        .WithTracing(
            tracing => tracing
                .AddNpgsql()
                .AddBeckett()
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
        );


    var host = builder.Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception during startup");
}
finally
{
    Log.CloseAndFlush();
}
