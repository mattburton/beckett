namespace Beckett.Projections;

public interface IHandleApply<T> where T : IApply, new()
{
    Task<T> Apply(T state, IMessageContext context, CancellationToken cancellationToken);
}
