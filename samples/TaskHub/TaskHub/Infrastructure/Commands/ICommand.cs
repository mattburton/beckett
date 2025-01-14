namespace TaskHub.Infrastructure.Commands;

public interface ICommand
{
    /// <summary>
    /// Execute the command and return one or more events as the result
    /// </summary>
    /// <returns>Event(s) produced by executing the command</returns>
    IEnumerable<object> Execute();
}

public interface ICommand<in TState> where TState : IApply
{
    /// <summary>
    /// Execute the command using the supplied state and return one or more events as the result
    /// </summary>
    /// <param name="state">
    /// The state of the stream the command is executed against projected to a read model that can be used to make
    /// decisions about what events to produce
    /// </param>
    /// <returns>Event(s) produced by executing the command</returns>
    IEnumerable<object> Execute(TState state);
}
