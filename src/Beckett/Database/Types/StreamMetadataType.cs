namespace Beckett.Database.Types;

public class StreamMetadataType
{
    public string StreamName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public long LatestPosition { get; set; }
    public long LatestGlobalPosition { get; set; }
    public long MessageCount { get; set; }

    private sealed class StreamMetadataTypeEqualityComparer : IEqualityComparer<StreamMetadataType>
    {
        public bool Equals(StreamMetadataType? x, StreamMetadataType? y)
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

        public int GetHashCode(StreamMetadataType obj)
        {
            return HashCode.Combine(obj.StreamName);
        }
    }

    public static IEqualityComparer<StreamMetadataType> Comparer { get; } = new StreamMetadataTypeEqualityComparer();
}