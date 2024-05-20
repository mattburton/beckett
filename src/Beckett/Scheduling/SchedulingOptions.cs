namespace Beckett.Scheduling;

public class SchedulingOptions
{
    public int BatchSize { get; set; } = 500;
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int RecurringBatchSize { get; set; } = 500;
    public TimeSpan RecurringPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
}
