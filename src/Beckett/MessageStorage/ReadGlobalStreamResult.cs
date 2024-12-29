namespace Beckett.MessageStorage;

public record ReadGlobalStreamResult(IReadOnlyList<GlobalStreamItem> Items);
