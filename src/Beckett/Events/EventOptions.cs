namespace Beckett.Events;

public class EventOptions
{
    public TimeSpan ScheduledEventPollingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public void Map<TEvent>(string name)
    {
        EventTypeMap.Map<TEvent>(name);
    }
}
