namespace Beckett;

public class StreamDoesNotExistException : Exception
{
    public StreamDoesNotExistException()
    {
    }

    public StreamDoesNotExistException(string? message) : base(message)
    {
    }
}
