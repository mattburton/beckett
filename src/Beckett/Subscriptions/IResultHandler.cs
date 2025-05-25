namespace Beckett.Subscriptions;

/// <summary>
/// If your subscription handler returns a result, you can optionally implement this interface to have Beckett handle
/// the result. By registering your implementation in the container it will be resolved when a result matching the type
/// is returned. The handler will be resolved from the scope wrapping the executing of the subscription handler itself,
/// so it can be registered as transient, scoped, or singleton as needed.
/// </summary>
public interface IResultHandler
{
    Task Handle(object result, CancellationToken cancellationToken);
}
