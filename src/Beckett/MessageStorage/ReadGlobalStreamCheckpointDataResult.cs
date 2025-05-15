namespace Beckett.MessageStorage;

public record ReadGlobalStreamCheckpointDataResult(IReadOnlyList<GlobalStreamItem> Items);
