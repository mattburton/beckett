using Beckett;

namespace Core.Projections;

public interface IProjection<T, TKey> where T : IApply, new()
{
    void Configure(IProjectionConfiguration<TKey> configuration);
    Task Create(T readModel, CancellationToken cancellationToken);
    Task<T?> Read(TKey key, CancellationToken cancellationToken);
    Task Update(T readModel, CancellationToken cancellationToken);
    Task Delete(TKey key, CancellationToken cancellationToken);
}
