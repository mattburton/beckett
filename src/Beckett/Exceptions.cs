namespace Beckett;

public class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException()
    {
    }

    public OptimisticConcurrencyException(string message) : base(message)
    {
    }
}

public class StreamAlreadyExistsException(string message) : Exception(message);

public class StreamDoesNotExistException(string message) : Exception(message);
