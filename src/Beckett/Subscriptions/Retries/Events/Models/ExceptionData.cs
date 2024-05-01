namespace Beckett.Subscriptions.Retries.Events.Models;

public record ExceptionData(string Type, string Message, ExceptionData? InnerException)
{
    public static ExceptionData From(Exception exception)
    {
        var type = exception.GetType().Name;
        var message = exception.Message;
        ExceptionData? innerException = null;

        if (exception.InnerException != null)
        {
            innerException = From(exception.InnerException);
        }

        return new ExceptionData(type, message, innerException);
    }
}
