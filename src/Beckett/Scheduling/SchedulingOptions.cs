namespace Beckett.Scheduling;

public class SchedulingOptions
{
    /// <summary>
    /// Configure the batch size to use when reading scheduled messages that are due to send. Defaults to 500.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Configure the polling interval for how often to check for scheduled messages that are due to send. Defaults to
    /// 10 seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
