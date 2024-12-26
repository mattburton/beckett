namespace Beckett;

public class StreamAlreadyExistsException : Exception
{
    public StreamAlreadyExistsException()
    {
    }

    public StreamAlreadyExistsException(string? message) : base(message)
    {
    }
}
