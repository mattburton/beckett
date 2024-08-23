namespace Beckett.Subscriptions.Retries;

public class RetryOptions
{
    /// <summary>
    /// Configure whether retry processing is enabled for this host. While it defaults to true, meaning that the
    /// services necessary for retries are registered and host services are running, one could choose to disable it to
    /// offload retry processing to another host process. In that case you'd have subscriptions enabled but retries
    /// disabled on one host and subscriptions disabled with retries enabled on another host process, both configured
    /// to use the same subscription group name.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Configure the initial delay before the first retry attempt when an error occurs processing a checkpoint.
    /// Defaults to 10 seconds.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Configure the maximum amount of time to wait between retry attempts. Defaults to 4 minutes.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Configure the polling interval to check for retries to process. When Postgres notifications are enabled (the
    /// default) this interval can be disabled by setting it to <c>TimeSpan.Zero</c> or increased and used as a backup
    /// sanity check to make sure that everything is being processed. Defaults to 10 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
