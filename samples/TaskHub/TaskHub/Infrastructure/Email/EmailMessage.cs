namespace TaskHub.Infrastructure.Email;

public record EmailMessage(string From, string To, string Subject, string Body);
