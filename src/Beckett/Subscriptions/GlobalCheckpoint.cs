namespace Beckett.Subscriptions;

public static class GlobalCheckpoint
{
    public const string Name = "$global";
    public const string Topic = "$checkpoint";
    public const string StreamId = "$all";
}
