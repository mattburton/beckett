using Core.Contracts;

namespace Core.Queries;

public interface IQueryHandler<in TQuery, TResult> : IQueryHandler where TQuery : IQuery<TResult>
{
    async Task<object?> IQueryHandler.Handle(object query, CancellationToken cancellationToken)
    {
        var result = await Handle((TQuery)query, cancellationToken);

        return result;
    }

    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);
}

public interface IQueryHandler
{
    Task<object?> Handle(object query, CancellationToken cancellationToken);
}
