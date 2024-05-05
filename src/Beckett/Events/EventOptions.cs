using Beckett.Events.Scheduling;

namespace Beckett.Events;

public class EventOptions
{
    public bool AllowDynamicTypeMapping { get; set; }

    public ScheduledEventOptions Scheduling { get; set; } = new();
}
