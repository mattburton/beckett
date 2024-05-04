namespace Beckett.Subscriptions.Retries.Events.Models;

public record ExceptionData(string Type, string Message, string? StackTrace, ExceptionData? InnerException)
{
    public static ExceptionData From(Exception exception)
    {
        var type = exception.GetType().Name;
        ExceptionData? innerException = null;

        if (exception.InnerException != null)
        {
            innerException = From(exception.InnerException);
        }

        return new ExceptionData(type, exception.Message, exception.StackTrace, innerException);
    }
}
