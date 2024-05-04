namespace Beckett.Subscriptions;

public interface IGlobalStreamConsumer
{
    void Run(CancellationToken cancellationToken);

    Task Consume(CancellationToken cancellationToken);
}
