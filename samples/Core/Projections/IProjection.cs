using Core.State;

namespace Core.Projections;

public interface IProjection<TState> where TState : class, IStateView, new()
{
    void Configure(IProjectionConfiguration configuration);
    Task<IReadOnlyList<TState>> Load(IEnumerable<object> keys, CancellationToken cancellationToken);
    object GetKey(TState state);
    void Save(TState state);
    void Delete(TState state);
    Task SaveChanges(CancellationToken cancellationToken);
}
