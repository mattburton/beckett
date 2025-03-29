using Beckett;
using Core.MessageHandling;

namespace Core.Projections;

public interface IProjection<TState, TKey> where TState : class, IApply, IHaveScenarios, new()
{
    void Configure(IProjectionConfiguration<TKey> configuration);
    Task Create(TState state, CancellationToken cancellationToken);
    Task<TState?> Read(TKey key, CancellationToken cancellationToken);
    Task Update(TState state, CancellationToken cancellationToken);
    Task Delete(TKey key, CancellationToken cancellationToken);
}
