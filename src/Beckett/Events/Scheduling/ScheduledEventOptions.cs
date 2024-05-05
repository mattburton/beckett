namespace Beckett.Events.Scheduling;

public class ScheduledEventOptions
{
    public int BatchSize { get; set; } = 100;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}
