using Beckett;
using Beckett.OpenTelemetry;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TodoList;
using TodoList.Infrastructure.Database;

var builder = Host.CreateApplicationBuilder(args);

await builder.AddTodoListDatabase();

builder.AddBeckett().TodoListComponent();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("todo-list-worker"))
    .WithMetrics(metrics => metrics
        .AddBeckett()
        .AddConsoleExporter()
    )
    .WithTracing(tracing => tracing
        .AddNpgsql()
        .AddBeckett()
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
    );

var host = builder.Build();

host.Run();
