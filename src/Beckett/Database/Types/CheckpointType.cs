namespace Beckett.Database.Types;

public class CheckpointType
{
    public int SubscriptionId { get; init; }
    public string StreamName { get; init; } = null!;
    public long StreamVersion { get; init; }
}
