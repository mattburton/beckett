namespace Beckett.Subscriptions;

public static class PollingAgent
{
    public static async Task Run(BeckettOptions options, SubscriptionStreamProcessor processor, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            processor.StartPolling(cancellationToken);

            await Task.Delay(options.Subscriptions.PollingInterval, cancellationToken);
        }
    }
}
