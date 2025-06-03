namespace Beckett.Subscriptions.PartitionStrategies;

public class GlobalStreamPartitionStrategy : IPartitionStrategy
{
    public static readonly GlobalStreamPartitionStrategy Instance = new();

    public string PartitionKey(IMessageContext _) => GlobalStream.Name;
}
