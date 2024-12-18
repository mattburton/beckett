using Beckett;
using Beckett.OpenTelemetry;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Taskmaster;
using Taskmaster.Infrastructure.Database;
using Taskmaster.TaskLists;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    await builder.AddTaskmasterDatabase();

    builder.Services.AddBeckett(
        options =>
        {
            options.WithSubscriptionGroup("Taskmaster");

            options.Subscriptions.MaxRetryCount<RetryableException>(10);

            options.Subscriptions.MaxRetryCount<TerminalException>(0);
        }
    ).WithSubscriptionsFrom(typeof(TaskList).Assembly);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("taskmaster-worker"))
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
