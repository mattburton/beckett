using TaskHub.Infrastructure.Email;

namespace TaskHub.Infrastructure.Tests;

public class FakeEmailService : IEmailService
{
    public EmailMessage? SentEmail { get; private set; }

    public Task Send(EmailMessage message, CancellationToken cancellationToken)
    {
        SentEmail = message;

        return Task.CompletedTask;
    }
}
