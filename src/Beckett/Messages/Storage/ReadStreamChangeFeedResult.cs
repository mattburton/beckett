namespace Beckett.Messages.Storage;

public readonly struct ReadStreamChangeFeedResult(IReadOnlyList<StreamChange> items)
{
    public IReadOnlyList<StreamChange> Items { get; } = items;
}
