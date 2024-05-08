namespace Beckett.Subscriptions.Initialization;

public interface ISubscriptionInitializer
{
    void Start(CancellationToken cancellationToken);
}
