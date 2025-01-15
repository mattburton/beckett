namespace Beckett.Projections;

public interface IProjectionMessageConfiguration<out TMessage, TKey>
{
    IProjectionMessageConfiguration<TMessage, TKey> IgnoreWhenNotFound();
    IProjectionMessageConfiguration<TMessage, TKey> IgnoreWhen(Predicate<TMessage> predicate);
}

public class ProjectionMessageConfiguration<TMessage, TKey> : IProjectionMessageConfiguration<TMessage, TKey>
{
    private ProjectionMessageConfiguration(
        ProjectionAction action,
        Func<object, TKey> key
    )
    {
        Configuration.Action = action;
        Configuration.Key = x => key(x)!;
    }

    internal ProjectionMessageConfiguration Configuration { get; } = new();

    internal static ProjectionMessageConfiguration<TMessage, TKey> Create(
        ProjectionAction action,
        Func<TMessage, TKey> keySelector
    )
    {
        return new ProjectionMessageConfiguration<TMessage, TKey>(action, x => keySelector((TMessage)x));
    }

    public IProjectionMessageConfiguration<TMessage, TKey> IgnoreWhenNotFound()
    {
        Configuration.IgnoreWhenNotFound = true;

        return this;
    }

    public IProjectionMessageConfiguration<TMessage, TKey> IgnoreWhen(Predicate<TMessage> predicate)
    {
        Configuration.IgnoreFilter = x => predicate((TMessage)x);

        return this;
    }
}

public class ProjectionMessageConfiguration
{
    internal ProjectionAction Action { get; set; }
    internal Func<object, object> Key { get; set; } = null!;
    internal bool IgnoreWhenNotFound { get; set; }
    internal Predicate<object> IgnoreFilter { get; set; } = _ => false;

    internal TKey GetKey<TKey>(object message) => (TKey)Key(message);
}
