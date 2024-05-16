namespace Beckett.Subscriptions;

public interface IGlobalStreamConsumer
{
    void Consume(CancellationToken cancellationToken);
}
