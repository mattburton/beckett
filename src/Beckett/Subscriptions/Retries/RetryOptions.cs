namespace Beckett.Subscriptions.Retries;

public class RetryOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(4);
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
