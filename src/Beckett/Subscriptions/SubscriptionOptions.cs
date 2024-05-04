namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 500;
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);
    public TimeSpan GlobalPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan SubscriptionPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
