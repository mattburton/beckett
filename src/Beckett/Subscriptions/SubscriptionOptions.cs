namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public bool Enabled { get; set; }
    public int BatchSize { get; set; } = 500;
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);
    public int GlobalBatchSize { get; set; } = 500;
    public TimeSpan GlobalPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RetryPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
