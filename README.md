# Beckett

The missing pieces to build event-driven applications. Event sourcing is a powerful pattern for building applications
but reading and writing events to an event store is only half of the equation. Beckett aims to fill in the gaps:

- Message Store - store messages (events, commands, jobs, etc...) in streams
  - PostgreSQL message storage is provided out of the box, but Beckett is designed to compliment your existing
    event or message store - use your own store and implement the `IMessageStorage` interface to integrate with Beckett
- Subscriptions - subscribe to messages and process them in order by stream
  - Horizontal scalability - use auto-scaling to have as many workers as needed processing messages in parallel where
    the work is distributed automatically across all available nodes without needing to manage the distribution by way
    of consumer groups or similar mechanisms
  - Retries - built-in retry support for failed messages - since messages are processed by stream per subscription,
    a failed message only blocks a single stream for a subscription at a time and the rest of the streams can continue
    processing for that subscription
- Scheduled / recurring messages - schedule messages to be sent at a future time with cancellation support, or create a
  recurring schedule to send messages at a regular interval using cron expressions for scheduled jobs, etc...
- Open Telemetry - built-in support to provide tracing and metrics
- Dashboard - view metrics, browse messages, with future plans for subscription management, retrying failed messages,
  and more

## Usage
After installing the `Beckett` package from NuGet, you can add Beckett to your worker like so:
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddBeckett();

var host = builder.Build();

host.Run();
```
Or an ASP.NET Core application along with the Beckett dashboard that is available in the `Beckett.Dashboard` NuGet package:
```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapBeckettDashboard("/beckett");

app.Run();
```
In this example, the dashboard will be available at `http://localhost:<port>/beckett`.

Also, since `MapBeckettDashboard` returns a `RouteGroupBuilder` instance you can further configure the route group as
needed to add authorization and so on using standard ASP.NET Core route group configuration methods.

## Configuration
There are a number of options available in Beckett which can be configured inline:
```csharp
builder.AddBeckett(
    options =>
    {
        options.ApplicationName = "todo-list-api";
    });
```
Or in `appsettings.json`:
```json
{
  "Beckett": {
    "ApplicationName": "todo-list-api"
  }
}
```

## Samples
This documentation is a work in progress - in the meantime take a look at the sample application to see all the features in action.
