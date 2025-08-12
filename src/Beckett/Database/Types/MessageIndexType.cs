namespace Beckett.Database.Types;

public class MessageIndexType
{
    public Guid Id { get; set; }
    public long GlobalPosition { get; set; }
    public string StreamName { get; set; } = null!;
    public long StreamPosition { get; set; }
    public string MessageTypeName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? Tenant { get; set; }
    public byte[] Metadata { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }

    private sealed class MessageMetadataTypeEqualityComparer : IEqualityComparer<MessageIndexType>
    {
        public bool Equals(MessageIndexType? x, MessageIndexType? y)
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

            return x.GlobalPosition == y.GlobalPosition && x.Id == y.Id;
        }

        public int GetHashCode(MessageIndexType obj)
        {
            return HashCode.Combine(obj.GlobalPosition, obj.Id);
        }
    }

    public static IEqualityComparer<MessageIndexType> Comparer { get; } = new MessageMetadataTypeEqualityComparer();
}
