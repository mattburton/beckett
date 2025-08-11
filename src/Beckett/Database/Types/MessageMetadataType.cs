namespace Beckett.Database.Types;

public class MessageMetadataType
{
    public Guid Id { get; set; }
    public long GlobalPosition { get; set; }
    public string StreamName { get; set; } = null!;
    public long StreamPosition { get; set; }
    public string Type { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? CorrelationId { get; set; }
    public string? Tenant { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    private sealed class MessageMetadataTypeEqualityComparer : IEqualityComparer<MessageMetadataType>
    {
        public bool Equals(MessageMetadataType? x, MessageMetadataType? y)
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

        public int GetHashCode(MessageMetadataType obj)
        {
            return HashCode.Combine(obj.GlobalPosition, obj.Id);
        }
    }

    public static IEqualityComparer<MessageMetadataType> Comparer { get; } = new MessageMetadataTypeEqualityComparer();
}