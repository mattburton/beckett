namespace Beckett.Messages;

public interface IMessageTypeProvider
{
    Type? FindMatchFor(Predicate<Type> criteria);
}
