using Beckett.Messages;
using TaskHub.Users.Contracts.Notifications;
using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.NotifyUserAdded.Tests;

public class UserAddedNotificationPublisherTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_user_added_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var notificationPublisher = new FakeNotificationPublisher();
            var expectedChannel = UserModule.StreamName(username);
            var expectedNotification = new UserAddedNotification(username, email);
            var userRegistered = new UserRegistered(username, email);
            var context = MessageContext.From(userRegistered) with { StreamName = expectedChannel};

            await UserAddedNotificationPublisher.UserRegistered(
                userRegistered,
                context,
                notificationPublisher,
                CancellationToken.None
            );

            Assert.NotNull(notificationPublisher.Received);
            Assert.Equal(expectedChannel, notificationPublisher.Received.Channel);
            Assert.Equivalent(expectedNotification, notificationPublisher.Received.Notification);
        }
    }
}
