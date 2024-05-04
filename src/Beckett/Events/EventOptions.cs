namespace Beckett.Events;

public class EventOptions
{
    public bool AllowDynamicTypeMapping { get; set; }
    public int ScheduledEventBatchSize { get; set; } = 100;
    public TimeSpan ScheduledEventPollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}
