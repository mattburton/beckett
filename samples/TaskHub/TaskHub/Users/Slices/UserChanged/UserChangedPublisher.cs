using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.UserChanged;

public static class UserChangedPublisher
{
    public static async Task Handle(
        IMessageContext context,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken
    )
    {
        if (context.Message is UserRegistered userRegistered)
        {
            await notificationPublisher.Publish(
                context.StreamName,
                new Notifications.UserChanged(userRegistered.Username, userRegistered.Email),
                cancellationToken
            );
        }
    }
}
