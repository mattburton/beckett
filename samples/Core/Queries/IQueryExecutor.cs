namespace Core.Queries;

public interface IQueryExecutor
{
    Task<TResult> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
