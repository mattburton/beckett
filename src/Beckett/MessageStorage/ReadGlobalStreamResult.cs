namespace Beckett.MessageStorage;

public readonly struct ReadGlobalStreamResult(IReadOnlyList<GlobalStreamItem> items)
{
    public IReadOnlyList<GlobalStreamItem> Items { get; } = items;
}
