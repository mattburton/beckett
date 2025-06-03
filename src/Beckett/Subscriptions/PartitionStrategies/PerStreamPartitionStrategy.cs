namespace Beckett.Subscriptions.PartitionStrategies;

public class PerStreamPartitionStrategy : IPartitionStrategy
{
    public static readonly PerStreamPartitionStrategy Instance = new();

    public string PartitionKey(IMessageContext context) => context.StreamName;
}
