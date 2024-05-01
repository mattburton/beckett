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
) : IEventData;

public static class EventContextDataExtensions
{
    public static IEventContext WithServices(this IEventData data, IServiceScope scope) => new EventContext(
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
