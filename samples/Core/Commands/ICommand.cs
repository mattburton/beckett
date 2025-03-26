using Beckett;

namespace Core.Commands;

public interface ICommand
{
    /// <summary>
    /// Specify the stream where the resulting events will be appended.
    /// </summary>
    /// <returns></returns>
    string StreamName();

    /// <summary>
    /// Override the expected version used when executing the command. By default the resulting events from the command
    /// will be appended to the stream using the current version of the stream as the expected version. If the command
    /// is responsible for creating a stream and should fail if it already exists, the value should be
    /// <see cref="Beckett.ExpectedVersion.StreamDoesNotExist" />. If the stream is supposed to exist and fail if not
    /// then the value should be <see cref="Beckett.ExpectedVersion.StreamExists" />. If you want to specify the actual
    /// expected version then it should be included in the command itself and then set the expected version to that
    /// value. Lastly, if you wish to opt out of optimistic concurrency checks altogether for the command set the value
    /// to <see cref="Beckett.ExpectedVersion.Any" /> and the event(s) will be appended to the stream regardless of its
    /// current version.
    /// </summary>
    public ExpectedVersion ExpectedVersion => ExpectedVersion.Any;

    /// <summary>
    /// Override the default read options which will result in the entire stream being read forwards from the beginning.
    /// Set this value if you want to read just a portion of the stream, only the last event, backwards, etc...
    /// </summary>
    public ReadOptions ReadOptions => ReadOptions.Default;

    /// <summary>
    /// Execute the command and return one or more events as the result
    /// </summary>
    /// <returns>Event(s) produced by executing the command</returns>
    IEnumerable<object> Execute();
}

public interface ICommand<in TState> where TState : IApply
{
    /// <summary>
    /// Specify the stream the state will be read from and the resulting events will be appended.
    /// </summary>
    /// <returns></returns>
    string StreamName();

    /// <summary>
    /// Override the expected version used when executing the command. By default the resulting events from the command
    /// will be appended to the stream using the current version of the stream as the expected version. If the command
    /// is responsible for creating a stream and should fail if it already exists, the value should be
    /// <see cref="Beckett.ExpectedVersion.StreamDoesNotExist" />. If the stream is supposed to exist and fail if not
    /// then the value should be <see cref="Beckett.ExpectedVersion.StreamExists" />. Lastly, if you wish to opt out of
    /// optimistic concurrency checks altogether for the command set the value to
    /// <see cref="Beckett.ExpectedVersion.Any" /> and the event(s) will be appended to the stream regardless of its
    /// current version.
    /// </summary>
    public ExpectedVersion ExpectedVersion => ExpectedVersion.Any;

    /// <summary>
    /// Override the default read options which will result in the entire stream being read forwards from the beginning.
    /// Set this value if you want to read just a portion of the stream, only the last event, backwards, etc...
    /// </summary>
    public ReadOptions ReadOptions => ReadOptions.Default;

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
