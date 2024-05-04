namespace Beckett;

public readonly struct ExpectedVersion
{
    private ExpectedVersion(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public static readonly ExpectedVersion StreamDoesNotExist = new(0);
    public static readonly ExpectedVersion StreamExists = new(-1);
    public static readonly ExpectedVersion Any = new(-2);

    public static ExpectedVersion For(long value) => new(value);
}
