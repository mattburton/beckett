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
    .Handler<OrderItemAddedHandler>();

var host = builder.Build();

host.Run();

public record OrderItemAdded(Guid OrderId, Guid ProductId, int Quantity);

public record InventoryAllocated(Guid ProductId, Guid OrderId, int Quantity);

public class OrderItemAddedHandler(IMessageStore messageStore) : IMessageHandler<OrderItemAdded>
{
    public async Task Handle(OrderItemAdded message, IMessageContext context, CancellationToken cancellationToken)
    {
        await messageStore.AppendToStream(
            $"inventory-{message.ProductId}",
            ExpectedVersion.Any,
            new InventoryAllocated(message.ProductId, message.OrderId, message.Quantity),
            cancellationToken
        );
    }
}
