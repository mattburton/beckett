using Beckett;

namespace Core.Queries;

public abstract class ProjectedStreamQueryHandler<TQuery, TResult>(
    IMessageStore messageStore
) : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult> where TResult : class, IApply, new()
{
    public virtual async Task<TResult?> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(StreamName(query), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<TResult>();
    }

    protected abstract string StreamName(TQuery query);
}
