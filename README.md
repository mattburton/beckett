# Beckett

Event sourcing is a powerful pattern for building applications but reading and writing events using an event store is only half of the equation. Beckett aims to fill in the gaps:

- Subscriptions - subscribe to messages and process them in order by stream
  - Projections, read models, event handlers - add asynchronous, event-driven behavior to your applications
  - Horizontal scalability - use auto-scaling to have as many workers as needed processing messages in parallel where the work is distributed automatically across all available nodes without needing to manage the distribution by way of consumer groups or similar mechanisms
  - Retries - built-in retry support for failed messages - since messages are processed in order by stream per subscription, a failed message only blocks a single stream for a subscription at a time and the rest of the streams can continue processing for that subscription
- Scheduled - schedule messages to be sent at a future time with cancellation support
- Open Telemetry - built-in support to provide tracing and metrics
- Dashboard - browse messages, retry failed subscription checkpoints
- Bring Your Own Event Store - Beckett provides a simple Postgres-based message store or use your own by implementing the `IMessageStorage` interface

## Example
We are building a warehouse management system and we need to allocate inventory to orders. The requirements are that allocation occurs when an item is added to an order:
```csharp
using Beckett;
using Beckett.Database;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("InventoryAllocation")!;

//ensure the Beckett database schema is up to date
await BeckettDatabase.Migrate(connectionString);

//configure the data source with support for Beckett
builder.Services.AddNpgsqlDataSource(connectionString, options => options.AddBeckett());

//register the subscription handler in the container
builder.Services.AddTransient<OrderItemAddedHandler>();

//add Beckett support to the host for the InventoryAllocation subscription group
var beckett = builder.AddBeckett(
    options => { options.WithSubscriptionGroup("InventoryAllocation"); }
);

//map message types
beckett.Map<OrderItemAdded>("order_item_added");
beckett.Map<InventoryAllocated>("inventory_allocated");

//add subscription handler
beckett.AddSubscription("order-item-inventory-allocation")
    .Message<OrderItemAdded>()
    .Handler<OrderItemAddedHandler>((handler, message, token) => handler.Handle(message, token));

var host = builder.Build();

host.Run();

public record OrderItemAdded(Guid OrderId, Guid ProductId, int Quantity);

public record InventoryAllocated(Guid ProductId, Guid OrderId, int Quantity);

public class OrderItemAddedHandler(IMessageStore messageStore)
{
    public async Task Handle(OrderItemAdded message, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            $"inventory-{message.ProductId}",
            ExpectedVersion.Any,
            new InventoryAllocated(message.ProductId, message.OrderId, message.Quantity),
            cancellationToken
        );
    }
}
```
In this example application we are handling the `OrderItemAdded` event with the `OrderItemAddedHandler` class. The host
has been configured to use the `InventoryAllocation` subscription group, and there can be as many instances of this host
running as necessary and the work will be divided among them automatically allowing you to take advantage of auto
scaling without limits. The handler will receive all `OrderItemAdded` messages written to the message store since it is
subscribed to that type in the `AddSubscription` call. When a message is received it is dispatched to the handler which
then writes an `InventoryAllocated` event to an `Inventory` stream to track allocated product inventory.

One of the guiding design principles of Beckett is keeping a minimal footprint - there should be as few references to
Beckett-provided types in application code as possible. Subscription handlers are registered as inline delegates that
can refer to handler instances that are resolved from the container or static functions. The only type from Beckett used
in the application code in this sample is `IMessageStore` which itself is optional if you're using your own message
store.

The call to `BeckettDatabase.Migrate` in the example is applying any outstanding migrations to the database that are
required by Beckett. If you wish to run the migrations separately using Flyway or similar tools then you can use the
`dump-migrations` shell script supplied in the root of the directory to create a single SQL file:

```shell
./dump-migrations.sh beckett 001.sql
```

In this case `beckett` is the schema you'd like to use in your database for the tables, functions, and types that
Beckett uses and `001.sql` is the path of the file you'd like to create.

## Dashboard
Beckett comes batteries-included with a dashboard to provide visibility into your system while it's running, retry
failed checkpoints, and so on:

<img width="1575" alt="Beckett Dashboard" src="https://github.com/user-attachments/assets/0dc5a445-111b-4552-a639-36b37779d094">

Adding the Beckett dashboard to an ASP.NET Core application is simple:

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapBeckettDashboard("/beckett");

app.Run();
```

In this example, the dashboard will be available at `http://localhost:<port>/beckett` and can be further configured
using standard ASP.NET Core route group configuration options - authorization, etc...
