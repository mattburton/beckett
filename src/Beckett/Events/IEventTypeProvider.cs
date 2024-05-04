namespace Beckett.Events;

public interface IEventTypeProvider
{
    Type? FindMatchFor(Predicate<Type> criteria);
}
