namespace Beckett.Subscriptions.Retries.Models;

public record Retry(Guid Id, string GroupName, string Name, string StreamName, long StreamPosition, ExceptionData Error);
