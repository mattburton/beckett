using Beckett;
using Core.Scenarios;
using Core.Streams;

namespace Core.Queries;

public abstract class StreamStateQueryHandler<TQuery, TState>(
    IStreamReader reader
) : IQueryHandler<TQuery, TState> where TQuery : IQuery<TState> where TState : class, IApply, IHaveScenarios, new()
{
    protected abstract IStreamName StreamName(TQuery query);

    public async Task<TState?> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(StreamName(query), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<TState>();
    }
}
