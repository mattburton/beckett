namespace Beckett.Projections;

public interface IProjectionMessageConfiguration<TKey>
{
    IProjectionMessageConfiguration<TKey> WithFilter<TMessage>(Predicate<TMessage> filter);
    IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound();
}

public class ProjectionMessageConfiguration<TKey>(
    Type messageType,
    ProjectionAction action,
    Func<object, TKey> key
) : IProjectionMessageConfiguration<TKey>
{
    internal Type MessageType { get; } = messageType;
    internal ProjectionAction Action { get; } = action;
    internal Func<object, TKey> Key { get; } = key;
    internal Predicate<object>? Filter { get; private set; }
    internal bool IgnoreIfNotFound { get; private set; }

    internal static ProjectionMessageConfiguration<TKey> Create<TMessage>(
        ProjectionAction action,
        Func<TMessage, TKey> keySelector
    )
    {
        return new ProjectionMessageConfiguration<TKey>(typeof(TMessage), action, x => keySelector((TMessage)x));
    }

    public IProjectionMessageConfiguration<TKey> WithFilter<TMessage>(Predicate<TMessage> filter)
    {
        Filter = x => filter((TMessage)x);

        return this;
    }

    public IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound()
    {
        IgnoreIfNotFound = true;

        return this;
    }
}
