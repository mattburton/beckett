namespace Beckett;

public readonly record struct ExpectedVersion(long Value)
{
    public static readonly ExpectedVersion StreamDoesNotExist = new(0);
    public static readonly ExpectedVersion StreamExists = new(-1);
    public static readonly ExpectedVersion Any = new(-2);
}
