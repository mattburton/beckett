namespace Beckett.Subscriptions.PartitionStrategies;

public interface IPartitionStrategy
{
    string PartitionKey(IMessageContext context);
}
