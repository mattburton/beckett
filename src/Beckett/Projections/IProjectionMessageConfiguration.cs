namespace Beckett.Projections;

public interface IProjectionMessageConfiguration<TKey>
{
    IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound();
}

public class ProjectionMessageConfiguration<TKey>(
    ProjectionAction action,
    Func<object, TKey> key
) : IProjectionMessageConfiguration<TKey>
{
    internal ProjectionAction Action { get; } = action;
    internal Func<object, TKey> Key { get; } = key;
    internal bool IgnoreIfNotFound { get; private set; }

    internal static ProjectionMessageConfiguration<TKey> Create<TMessage>(
        ProjectionAction action,
        Func<TMessage, TKey> keySelector
    )
    {
        return new ProjectionMessageConfiguration<TKey>(action, x => keySelector((TMessage)x));
    }

    public IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound()
    {
        IgnoreIfNotFound = true;

        return this;
    }
}
