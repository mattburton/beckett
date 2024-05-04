namespace Beckett.Database.Notifications;

public interface IPostgresNotificationHandler
{
    string Channel { get; }

    void Handle(string payload, CancellationToken cancellationToken);
}
