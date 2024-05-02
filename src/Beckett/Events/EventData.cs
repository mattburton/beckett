using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

public readonly record struct EventData(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Data,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp
);

public static class EventDataExtensions
{
    public static IEventContext WithServices(this EventData data, IServiceScope scope) => new EventContext(
        data.Id,
        data.StreamName,
        data.StreamPosition,
        data.GlobalPosition,
        data.Type,
        data.Data,
        data.Metadata,
        data.Timestamp,
        scope
    );
}
