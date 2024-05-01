using Beckett.Events;

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

        return new ScheduledEvent(@event, deliverAt);
    }
}
