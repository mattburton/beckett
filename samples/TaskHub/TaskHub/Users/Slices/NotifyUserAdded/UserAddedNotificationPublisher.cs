using TaskHub.Users.Contracts.Notifications;
using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.NotifyUserAdded;

public static class UserAddedNotificationPublisher
{
    public static async Task UserRegistered(
        UserRegistered message,
        IMessageContext context,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken
    )
    {
        await notificationPublisher.Publish(
            context.StreamName,
            new UserAddedNotification(message.Username, message.Email),
            cancellationToken
        );
    }
}
