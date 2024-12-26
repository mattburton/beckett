namespace Beckett.Projections;

public interface IHandleApply<T> where T : IApply, new()
{
    Task<T> Apply(T state, object message, CancellationToken cancellationToken);
}
