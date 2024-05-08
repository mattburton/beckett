namespace Beckett.Messages.Scheduling;

public class ScheduledMessageOptions
{
    public int BatchSize { get; set; } = 500;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
}
