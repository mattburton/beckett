namespace Beckett.MessageStorage;

public record ReadIndexBatchResult(IReadOnlyList<IndexBatchItem> Items);
