namespace Beckett.Events;

public class EventOptions
{
    public EventOptions()
    {
        TypeMap = new EventTypeMap(this);
    }

    internal EventTypeMap TypeMap { get; }

    public bool AllowDynamicTypeMapping { get; set; }
    public int ScheduledEventBatchSize { get; set; } = 100;
    public TimeSpan ScheduledEventPollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public void Map<TEvent>(string name) => TypeMap.Map<TEvent>(name);
}
