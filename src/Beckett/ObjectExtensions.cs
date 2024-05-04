using Beckett.Events;
using Beckett.ScheduledEvents;

namespace Beckett;

public static class ObjectExtensions
{
    public static object DelayFor(this object @event, TimeSpan delay)
    {
        var deliverAt = DateTimeOffset.UtcNow.Add(delay);

        return ScheduleAt(@event, deliverAt);
    }

    public static object ScheduleAt(this object @event, DateTimeOffset deliverAt)
    {
        if (deliverAt < DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("When scheduling an event the delivery time must be in the future");
        }

        return new ScheduledEventWrapper(@event, deliverAt);
    }

    public static object WithMetadata(this object @event, Dictionary<string, object> metadata)
    {
        return new MetadataEventWrapper(@event, metadata);
    }
}
