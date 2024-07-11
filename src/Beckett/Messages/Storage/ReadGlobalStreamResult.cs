namespace Beckett.Messages.Storage;

public readonly struct ReadGlobalStreamResult(IReadOnlyList<GlobalStreamItem> items)
{
    public IReadOnlyList<GlobalStreamItem> Items { get; } = items;
}
