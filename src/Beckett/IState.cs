namespace Beckett;

public interface IState
{
    void Apply(object @event);
}