namespace Beckett.ScheduledEvents;

public class ScheduledEventOptions
{
    public int BatchSize { get; set; } = 500;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
}
