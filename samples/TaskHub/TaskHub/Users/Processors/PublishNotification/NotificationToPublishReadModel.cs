using Contracts.Users.Notifications;
using TaskHub.Users.Events;

namespace TaskHub.Users.Processors.PublishNotification;

[ReadModel]
public partial class NotificationToPublishReadModel
{
    private string UserName { get; set; } = null!;
    private string Email { get; set; } = null!;
    private bool Deleted { get; set; }

    private void Apply(UserRegistered m)
    {
        UserName = m.Username;
        Email = m.Email;
    }

    private void Apply(UserDeleted _)
    {
        Deleted = true;
    }

    public INotification ToNotification()
    {
        if (Deleted)
        {
            return new UserDeletedNotification(UserName);
        }

        return new UserCreatedNotification(UserName, Email);
    }
}
