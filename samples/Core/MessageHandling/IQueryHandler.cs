using Core.Contracts;

namespace Core.MessageHandling;

public interface IQueryHandler<in TQuery, TResult> : IQueryHandler where TQuery : IQuery<TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken);

    async Task<object?> IQueryHandler.Handle(object query, CancellationToken cancellationToken)
    {
        var result = await Handle((TQuery)query, cancellationToken);

        return result;
    }
}

public interface IQueryHandler
{
    Task<object?> Handle(object query, CancellationToken cancellationToken);
}
