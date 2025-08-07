namespace Beckett;

/// <summary>
/// Exception that is thrown when concurrent writes to the same stream occur, and one writer loses. The write will be
/// retried immediately up to three times, after which the exception will bubble up. This primarily happens when using
/// <see cref="ExpectedVersion.Any"/> or <see cref="ExpectedVersion.StreamExists"/> as the expected version when
/// appending messages to a stream and as such it is recommended to favor specifying the expected version explicitly
/// when appending messages to a stream or use <see cref="ExpectedVersion.StreamDoesNotExist"/> when starting a new
/// stream.
/// </summary>
public class StreamConcurrentWriteException : Exception
{
    public StreamConcurrentWriteException()
    {
    }

    public StreamConcurrentWriteException(string? message) : base(message)
    {
    }
}
