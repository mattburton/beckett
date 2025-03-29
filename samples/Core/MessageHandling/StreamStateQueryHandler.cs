using Beckett;
using Core.Contracts;
using Core.Streams;

namespace Core.MessageHandling;

public abstract class StreamStateQueryHandler<TQuery, TState, TResult>(
    IStreamReader reader
) : IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult> where TState : class, IApply, IHaveScenarios, new()
{
    protected abstract IStreamName StreamName(TQuery query);

    protected abstract TResult Map(TState state);

    public async Task<TResult> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(StreamName(query), cancellationToken);

        if (stream.IsEmpty)
        {
            return default!;
        }

        var state = stream.ProjectTo<TState>();

        return Map(state);
    }
}
