namespace Beckett.Subscriptions.Retries;

public class RetryOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
