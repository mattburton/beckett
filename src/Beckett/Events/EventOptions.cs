namespace Beckett.Events;

public class EventOptions
{
    internal EventTypeMap TypeMap { get; } = new();

    public int ScheduledEventBatchSize { get; set; } = 100;
    public TimeSpan ScheduledEventPollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public void Map<TEvent>(string name) => TypeMap.Map<TEvent>(name);
}
