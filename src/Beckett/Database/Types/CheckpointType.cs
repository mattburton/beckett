namespace Beckett.Database.Types;

public class CheckpointType
{
    public string GroupName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string StreamName { get; set; } = null!;
    public long StreamVersion { get; set; }
    public long StreamPosition { get; set; }

    private sealed class CheckpointTypeEqualityComparer : IEqualityComparer<CheckpointType>
    {
        public bool Equals(CheckpointType? x, CheckpointType? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.GroupName == y.GroupName && x.Name == y.Name && x.StreamName == y.StreamName;
        }

        public int GetHashCode(CheckpointType obj)
        {
            return HashCode.Combine(obj.GroupName, obj.Name, obj.StreamName);
        }
    }

    public static IEqualityComparer<CheckpointType> Comparer { get; } = new CheckpointTypeEqualityComparer();
}
