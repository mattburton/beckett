# Beckett

Event sourcing is a powerful pattern for building applications but reading and writing events to an event store is only half of the equation. Beckett aims to fill in the gaps:

- Subscriptions - subscribe to messages and process them in order by stream
  - Horizontal scalability - use auto-scaling to have as many workers as needed processing messages in parallel where the work is distributed automatically across all available nodes without needing to manage the distribution by way of consumer groups or similar mechanisms
  - Retries - built-in retry support for failed messages - since messages are processed in order by stream per subscription, a failed message only blocks a single stream for a subscription at a time and the rest of the streams can continue processing for that subscription
- Scheduled / recurring messages - schedule messages to be sent at a future time with cancellation support, or create a recurring schedule to send messages at a regular interval using cron expressions for scheduled jobs, etc...
- Open Telemetry - built-in support to provide tracing and metrics
- Dashboard - browse messages, monitor metrics, retry failed subscriptions
- Bring Your Own Event Store - Beckett provides a simple Postgres-based message store or use your own by implementing the `IMessageStorage` interface

## Usage
After installing the `Beckett` package from NuGet, you can add Beckett to your worker like so:
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddBeckett();

var host = builder.Build();

host.Run();
```
Add the Beckett dashboard to an ASP.NET Core application:
```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapBeckettDashboard("/beckett");

app.Run();
```
In this example, the dashboard will be available at `http://localhost:<port>/beckett` and can be further configured using standard ASP.NET Core route group configuration options - authorization, etc...

## Configuration
There are a number of options available in Beckett which can be configured inline or via standard .NET configuration:
```csharp
builder.AddBeckett(
    options =>
    {
        options.WithSubscriptions("TodoList");
    });
```

## Beckett?

Named after the Quantum Leap character [Sam Beckett](https://en.wikipedia.org/wiki/Sam_Beckett). Looking through a list of sci-fi character names it felt right. There was also going to be a whole thing about time traveling and event sourcing, but I'll spare you for now.
