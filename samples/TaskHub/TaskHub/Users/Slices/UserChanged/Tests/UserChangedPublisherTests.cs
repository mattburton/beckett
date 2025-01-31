using Beckett.Messages;
using TaskHub.Users.Events;
using TaskHub.Users.Notifications;

namespace TaskHub.Users.Slices.UserChanged.Tests;

public class UserChangedPublisherTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var notificationPublisher = new FakeNotificationPublisher();
            var expectedChannel = UserModule.StreamName(username);
            var expectedNotification = new Notifications.UserChanged(username, email);
            var userRegistered = new UserRegistered(username, email);
            var context = MessageContext.From(userRegistered) with { StreamName = expectedChannel};

            await UserChangedPublisher.Handle(
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
