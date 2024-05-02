using Microsoft.Extensions.DependencyInjection;

namespace Beckett.Events;

internal readonly record struct EventContext(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Data,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp,
    IServiceScope Services
) : IEventContext;
