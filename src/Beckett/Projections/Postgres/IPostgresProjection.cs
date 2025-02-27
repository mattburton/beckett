using Npgsql;

namespace Beckett.Projections.Postgres;

public interface IPostgresProjection<T, TKey> where T : IApply, new()
{
    void Configure(IProjectionConfiguration<TKey> configuration);
    NpgsqlBatchCommand Create(T state);
    Task<IReadOnlyList<T>> ReadAll(TKey[] keys, CancellationToken cancellationToken);
    NpgsqlBatchCommand Update(T state);
    NpgsqlBatchCommand Delete(TKey key);
}
