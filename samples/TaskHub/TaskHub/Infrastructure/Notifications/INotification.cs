namespace TaskHub.Infrastructure.Notifications;

public interface INotification
{
    Operation Operation { get; }
}
