namespace Beckett.Storage;

public record ReadIndexBatchResult(IReadOnlyList<IndexBatchItem> Items);
