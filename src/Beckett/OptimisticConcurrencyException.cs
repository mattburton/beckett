namespace Beckett;

public class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException()
    {
    }

    public OptimisticConcurrencyException(string? message) : base(message)
    {
    }
}
