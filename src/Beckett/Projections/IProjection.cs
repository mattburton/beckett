namespace Beckett.Projections;

public interface IProjection<T, TKey> where T : IApply, new()
{
    void Configure(IProjectionConfiguration<TKey> configuration);
    Task<T?> Load(TKey key, CancellationToken cancellationToken);
    Task Create(T state, CancellationToken cancellationToken);
    Task Update(T state, CancellationToken cancellationToken);
    Task Delete(T state, CancellationToken cancellationToken);
}
