namespace TaskHub.Infrastructure.Email;

public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task Send(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Sent email: {Email}", message);

        return Task.CompletedTask;
    }
}
