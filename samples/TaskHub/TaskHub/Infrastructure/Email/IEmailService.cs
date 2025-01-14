namespace TaskHub.Infrastructure.Email;

public interface IEmailService
{
    Task Send(EmailMessage message, CancellationToken cancellationToken);
}
