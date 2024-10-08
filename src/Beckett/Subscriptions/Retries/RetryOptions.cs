namespace Beckett.Subscriptions.Retries;

public class RetryOptions
{
    /// <summary>
    /// Configure the initial delay before the first retry attempt when an error occurs processing a checkpoint.
    /// Defaults to 10 seconds.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Configure the maximum amount of time to wait between retry attempts. Defaults to 4 minutes.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(4);
}
