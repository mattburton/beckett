using Beckett;
using Beckett.OpenTelemetry;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TaskHub;
using TaskHub.Infrastructure.Database;
using TaskHub.Infrastructure.DependencyInjection;
using TaskHub.Infrastructure.Modules;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    await builder.AddTaskHubDatabase();

    builder.Services.ConfigureServices();

    builder.Services.AddBeckett(
        options =>
        {
            options.WithSubscriptionGroup("TaskHub");

            options.Postgres.UseNotificationsConnectionString(
                builder.Configuration.GetConnectionString("Notifications") ??
                throw new Exception("Missing Notifications connection string")
            );
        }
    ).WithSubscriptionsFrom(TaskHubAssembly.Instance);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("taskhub-worker"))
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
