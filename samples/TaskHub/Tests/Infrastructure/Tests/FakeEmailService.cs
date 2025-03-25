using TaskHub.Infrastructure.Email;

namespace Tests.Infrastructure.Tests;

public class FakeEmailService : IEmailService
{
    public EmailMessage? Received { get; private set; }

    public Task Send(EmailMessage message, CancellationToken cancellationToken)
    {
        Received = message;

        return Task.CompletedTask;
    }
}
