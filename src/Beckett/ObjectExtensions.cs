using Beckett.Messages;
using Beckett.Messages.Scheduling;

namespace Beckett;

public static class ObjectExtensions
{
    public static object DelayFor(this object message, TimeSpan delay)
    {
        var deliverAt = DateTimeOffset.UtcNow.Add(delay);

        return ScheduleAt(message, deliverAt);
    }

    public static object ScheduleAt(this object message, DateTimeOffset deliverAt)
    {
        if (deliverAt < DateTimeOffset.UtcNow)
        {
            throw new Exception("When scheduling a message the delivery time must be in the future");
        }

        return new ScheduledMessageWrapper(message, deliverAt);
    }

    public static object WithMetadata(this object message, Dictionary<string, object> metadata)
    {
        return new MessageMetadataWrapper(message, metadata);
    }
}
