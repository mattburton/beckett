namespace TaskHub.Infrastructure.Commands;

public class CommandOptions
{
    /// <summary>
    ///     Override the expected version used when executing the command. By default the resulting events from the command
    ///     will be appended to the stream using the current version of the stream as the expected version. If the command
    ///     is responsible for creating a stream and should fail if it already exists, the value should be
    ///     <see cref="Beckett.ExpectedVersion.StreamDoesNotExist" />. If the stream is supposed to exist and fail if not
    ///     then the value should be <see cref="Beckett.ExpectedVersion.StreamExists" />. Lastly, if you wish to opt out of
    ///     optimistic concurrency checks altogether for the command set the value to
    ///     <see cref="Beckett.ExpectedVersion.Any" /> and the event(s) will be appended to the stream regardless of its
    ///     current version.
    /// </summary>
    public ExpectedVersion? ExpectedVersion { get; set; }

    /// <summary>
    ///     Override the default read options which will result in the entire stream being read forwards from the beginning.
    ///     Set this value if you want to read just a portion of the stream, only the last event, backwards, etc...
    /// </summary>
    public ReadOptions? ReadOptions { get; set; }
}
