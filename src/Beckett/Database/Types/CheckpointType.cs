namespace Beckett.Database.Types;

public class CheckpointType
{
    public string GroupName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string StreamName { get; init; } = null!;
    public long StreamVersion { get; init; }
}
