namespace TaskHub.Infrastructure.Queries;

public interface IQueryExecutor
{
    Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken) where TResult : class;
}
