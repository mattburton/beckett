using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.UserAddedNotification;

public static class UserAddedNotificationPublisher
{
    public static async Task UserRegistered(
        UserRegistered message,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        await messageStore.AppendToStream(
            UserModule.NotificationStreamName(message.Username),
            ExpectedVersion.Any,
            new Contracts.Notifications.UserAddedNotification(message.Username, message.Email),
            cancellationToken
        );
    }
}
