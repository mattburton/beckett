using Beckett;
using Beckett.Database;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("app")!;

await PostgresMigrator.UpgradeSchema(connectionString);

builder.Services.AddNpgsqlDataSource(connectionString, options => options.AddBeckett());

builder.Services.AddTransient<OrderItemAddedHandler>();

var beckett = builder.AddBeckett(
    options => { options.WithSubscriptionGroup("InventoryAllocation"); }
);

beckett.AddSubscription("order-item-inventory-allocation")
    .Message<OrderItemAdded>()
    .Handler<OrderItemAddedHandler>((handler, message, token) => handler.Handle(message, token));

beckett.AddRecurringMessage(
    "Send sample OrderItemAdded event each minute",
    "* * * * *",
    "order-items",
    new OrderItemAdded(Guid.NewGuid(), Guid.NewGuid(), 1)
);

var host = builder.Build();

host.Run();

public record OrderItemAdded(Guid OrderId, Guid ProductId, int Quantity);

public record InventoryAllocated(Guid ProductId, Guid OrderId, int Quantity);

public class OrderItemAddedHandler(IMessageStore messageStore)
{
    public async Task Handle(OrderItemAdded message, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            $"Inventory-{message.ProductId}",
            ExpectedVersion.Any,
            new InventoryAllocated(message.ProductId, message.OrderId, message.Quantity),
            cancellationToken
        );
    }
}
