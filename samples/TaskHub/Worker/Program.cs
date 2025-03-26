using Beckett;
using Beckett.OpenTelemetry;
using Core.DependencyInjection;
using Core.Modules;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TaskHub;
using TaskHub.Infrastructure.Database;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) => configuration.ReadFrom.Configuration(builder.Configuration));

    //default host shutdown timeout is 30 seconds - make sure that it's set higher than your reservation timeout
    builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(90));

    await builder.AddTaskHubDatabase();

    builder.Services.ConfigureServices();

    builder.Services.AddBeckett(
        options =>
        {
            options.WithSubscriptionGroup("TaskHub");

            //default reservation timeout is 5 minutes - we can lower that for the purposes of this demo
            options.Subscriptions.ReservationTimeout = TimeSpan.FromSeconds(60);
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
