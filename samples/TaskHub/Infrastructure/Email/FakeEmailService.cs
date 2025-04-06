namespace Infrastructure.Email;

public class FakeEmailService : IEmailService
{
    public EmailMessage? Received { get; private set; }

    public Task Send(EmailMessage message, CancellationToken cancellationToken)
    {
        Received = message;

        return Task.CompletedTask;
    }
}
