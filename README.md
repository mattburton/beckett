# Beckett

Beckett was created to fill a particular need when building event-driven and event-sourced applications - once events have been written to an event store, now what? I can read them out again, but how do I trigger async, event-driven workflows using them, or build reports, or send emails, etc...?

What Beckett brings to the table is a unique take on subscriptions - the ability to subscribe to new events that are written and process them asynchronously in a simple, reliable, and scalable way making it easy to add event handlers, projections, etc... to your system to actually _do_ things with the events you're capturing. There are existing solutions in this space, and your event store might provide its own - EventStoreDB persistent subscriptions for example - but read on for what makes the Beckett approach different.

## Scalability

Kafka consumers can be scaled via consumer groups, but the number of consumers in each group is limited by the number of partitions in the topic being consumed. Processing messages at the stream-level removes this limitation. You can think of streams as topics with an infinite number of partitions, and Beckett's subscription groups as horizontally scalable consumer groups without any limitations beyond the available resources.

Each host process is assigned a subscription group in a Beckett application.  This is a unique name - "OrderProcessing", "Reporting" - which links any instance of the host to that group. Any subscriptions that are registered within that host will then be processed by all the instances within that group where the load is evenly distributed across the nodes. Nodes can come and go as needed, allowing for flexible scaling policies without any need for rebalancing or reconfiguration.

## Error Handling & Retries

When processing messages the majority of issues are temporal - network / database connectivity, etc... - where a retry or two after some delay will result in success. There are also times when errors occur that are isolated to a single user, workflow, etc... where we don't want to hold up processing all other messages that are unrelated to that use case waiting on retries.

Beckett processes subscriptions at the stream-level, using checkpoints to track progress. A checkpoint is scoped to a subscription group + subscription + stream and keeps track of the current version (max position) of the stream as well as the last position that was processed.

If an error occurs processing a checkpoint then it can be retried in isolation from all other checkpoints, so if a sales order fails to allocate inventory because what we thought we had in stock doesn't match reality all other sales orders will continue to process while we resolve the issue. Let's say we needed to reach out to the customer to offer them an alternative that's in stock, and it takes a day or two to hear back from them. By that point the max retries will have been exhausted and the checkpoint will be in a failed state. Using the Beckett dashboard we can manually retry the failed checkpoint so that it can continue processing now that the situation has been resolved.

## Bring Your Own Event Store

Beckett provides a simple Postgres-based message store or you can implement the `IMessageStorage` interface to integrate with your existing message store. In that configuration Beckett will use a Postgres database for its storage - checkpoints, subscriptions, etc... - and will read and write messages using the external store. The primary usage will be reading new messages and streams for processing, but for retries and scheduled/recurring messages Beckett will also write to your store.

## Scheduled & Recurring Messages

You may have noticed the use of "message" vs "event" and "message store" vs "event store" up until now. Event sourcing is the primary target for Beckett, but it is flexible enough to handle other use cases that would typically require bringing in additional infrastructure such as Hangfire or Quartz.NET allowing you to build a complete system with fewer moving parts.

To that end Beckett has embraced "message" and "message store" as the generic terms it uses to refer to things. Commands, jobs, etc... are all things that Beckett can support. Asynchronous commands can be written to streams where a subscription handles them. One-off jobs / tasks can be treated in the same manner. To replace cron jobs and scheduled jobs / tasks, use Beckett's recurring messages, where a message is delivered to a stream based on a schedule configured by a cron expression. In order to schedule / defer a message to a future point in time use scheduled messages - send a reminder email 10 days from now, etc... If you need to cancel the message before then that is supported as well.

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
        options.WithSubscriptions("TodoList");
    });
```
Or in `appsettings.json`:
```json
{
  "Beckett": {
    "Subscriptions": {
      "Enabled": true,
      "GroupName": "TodoList"
    }
  }
}
```

## Beckett?

Named after the Quantum Leap character [Sam Beckett](https://en.wikipedia.org/wiki/Sam_Beckett). Looking through a list of sci-fi character names it felt right. There was also going to be a whole thing about time traveling and event sourcing, but I'll spare you for now.
