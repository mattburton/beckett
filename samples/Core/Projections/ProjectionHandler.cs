using Beckett;
using Core.State;

namespace Core.Projections;

public static class ProjectionHandler<TState> where TState : class, IStateView, new()
{
    public static async Task Handle(
        IReadOnlyList<IMessageContext> batch,
        IProjector<TState> projector,
        CancellationToken cancellationToken
    ) => await projector.Project(batch, cancellationToken);
}
