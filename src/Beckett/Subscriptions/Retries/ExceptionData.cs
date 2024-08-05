using System.Text.Json;

namespace Beckett.Subscriptions.Retries;

public record ExceptionData(string Type, string Message, List<string>? StackTrace, ExceptionData? InnerException)
{
    public static ExceptionData From(Exception exception)
    {
        var type = exception.GetType().FullName!;

        var stackTrace = exception.StackTrace?.Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            .Where(x => !x.StartsWith("at Beckett."))
            .ToList();

        ExceptionData? innerException = null;

        if (exception.InnerException != null)
        {
            innerException = From(exception.InnerException);
        }

        return new ExceptionData(
            type,
            exception.Message,
            stackTrace,
            innerException
        );
    }

    public static ExceptionData? FromJson(string json) => JsonSerializer.Deserialize<ExceptionData>(json);

    public string ToJson() => JsonSerializer.Serialize(this);
}
