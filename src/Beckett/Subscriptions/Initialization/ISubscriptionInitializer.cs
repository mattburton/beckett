namespace Beckett.Subscriptions.Initialization;

public interface ISubscriptionInitializer
{
    Task Initialize(CancellationToken cancellationToken);
}
