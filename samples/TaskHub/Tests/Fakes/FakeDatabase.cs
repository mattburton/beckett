using TaskHub.Infrastructure.Database;

namespace Tests.Fakes;

public class FakeDatabase : IDatabase
{
    private object? _returnValue;

    public object? ExecutedQuery { get; private set; }

    public void Returns<T>(T value)
    {
        _returnValue = value;
    }

    public Task<T> Execute<T>(IDatabaseQuery<T> query, CancellationToken cancellationToken)
    {
        ExecutedQuery = query;

        return _returnValue == null ? Task.FromResult(default(T))! : Task.FromResult((T)_returnValue);
    }
}
