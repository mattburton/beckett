using Beckett.Subscriptions.Retries;

namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public string GroupName { get; set; } = "default";
    public bool Enabled { get; set; }
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);
    public TimeSpan CheckpointReservationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int CheckpointReservationRecoveryBatchSize { get; set; } = 100;
    public TimeSpan CheckpointReservationRecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int GlobalStreamBatchSize { get; set; } = 500;
    public int GlobalStreamEmptyResultsMaxRetryCount { get; set; } = 1;
    public TimeSpan GlobalStreamEmptyResultsRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan GlobalStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int InitializationBatchSize { get; set; } = 1000;
    public int SubscriptionStreamBatchSize { get; set; } = 500;
    public TimeSpan SubscriptionStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    public RetryOptions Retries { get; set; } = new();
}
