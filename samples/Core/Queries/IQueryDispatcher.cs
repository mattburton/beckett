using Core.Contracts;

namespace Core.Queries;

public interface IQueryDispatcher
{
    Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
