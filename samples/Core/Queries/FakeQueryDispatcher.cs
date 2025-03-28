using Core.Contracts;

namespace Core.Queries;

public class FakeQueryDispatcher : IQueryDispatcher
{
    private readonly Dictionary<object, object?> _expectedResults = [];

    public Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        return _expectedResults.TryGetValue(query, out var result)
            ? Task.FromResult((TResult)result!)
            : Task.FromResult<TResult>(default!);
    }

    public void Returns<TResult>(IQuery<TResult> query, TResult? result) => _expectedResults[query] = result;
}
