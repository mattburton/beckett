namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 500;
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);
    public int BufferSize { get; set; } = 10000;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(10);
    public int MaxRetryCount { get; set; } = 10;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
