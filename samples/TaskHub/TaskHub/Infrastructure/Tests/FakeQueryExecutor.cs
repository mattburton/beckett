namespace TaskHub.Infrastructure.Tests;

public class FakeQueryExecutor : IQueryExecutor
{
    private readonly Dictionary<object, object?> _expectedResults = [];

    public Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        where TResult : class =>
        _expectedResults.TryGetValue(query, out var result)
            ? Task.FromResult((TResult?)result)
            : Task.FromResult<TResult?>(null);

    public void Returns<TResult>(IQuery<TResult> query, TResult? result) where TResult : class =>
        _expectedResults[query] = result;
}