namespace Core.Projections;

public interface IProjectionMessageConfiguration<out TMessage>
{
    IProjectionMessageConfiguration<TMessage> IgnoreWhenNotFound();
    IProjectionMessageConfiguration<TMessage> Where(Predicate<TMessage> predicate);
}

public class ProjectionMessageConfiguration<TMessage> : IProjectionMessageConfiguration<TMessage>
{
    private ProjectionMessageConfiguration(
        ProjectionAction action,
        Func<object, object> key
    )
    {
        Configuration.Action = action;
        Configuration.Key = key;
    }

    internal ProjectionMessageConfiguration Configuration { get; } = new();

    internal static ProjectionMessageConfiguration<TMessage> Create(
        ProjectionAction action,
        Func<TMessage, object> keySelector
    )
    {
        return new ProjectionMessageConfiguration<TMessage>(action, x => keySelector((TMessage)x));
    }

    public IProjectionMessageConfiguration<TMessage> IgnoreWhenNotFound()
    {
        Configuration.IgnoreWhenNotFound = true;

        return this;
    }

    public IProjectionMessageConfiguration<TMessage> Where(Predicate<TMessage> predicate)
    {
        Configuration.WherePredicate = x => predicate((TMessage)x);

        return this;
    }
}

public class ProjectionMessageConfiguration
{
    internal ProjectionAction Action { get; set; }
    internal Func<object, object> Key { get; set; } = null!;
    internal bool IgnoreWhenNotFound { get; set; }
    internal Predicate<object> WherePredicate { get; set; } = _ => true;

    internal object GetKey(object message) => Key(message);
}
