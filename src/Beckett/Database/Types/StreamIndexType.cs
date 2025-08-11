namespace Beckett.Database.Types;

public class StreamIndexType
{
    public string StreamName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public long LatestPosition { get; set; }
    public long LatestGlobalPosition { get; set; }
    public long MessageCount { get; set; }

    private sealed class StreamMetadataTypeEqualityComparer : IEqualityComparer<StreamIndexType>
    {
        public bool Equals(StreamIndexType? x, StreamIndexType? y)
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

            return x.StreamName == y.StreamName;
        }

        public int GetHashCode(StreamIndexType obj)
        {
            return HashCode.Combine(obj.StreamName);
        }
    }

    public static IEqualityComparer<StreamIndexType> Comparer { get; } = new StreamMetadataTypeEqualityComparer();
}
