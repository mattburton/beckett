using Beckett.Messages;
using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.PublishNotification.Tests;

public class UserNotificationPublisherTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var notificationPublisher = new FakeNotificationPublisher();
            var messageStore = new FakeMessageStore();
            var expectedChannel = UserModule.StreamName(username);
            var expectedNotification = new Notifications.User(Operation.Create, username, email);
            var userRegistered = new UserRegistered(username, email);
            var context = MessageContext.From(userRegistered) with { StreamName = expectedChannel};
            messageStore.HasExistingMessages(expectedChannel, userRegistered);

            await UserNotificationPublisher.Handle(
                context,
                messageStore,
                notificationPublisher,
                CancellationToken.None
            );

            Assert.NotNull(notificationPublisher.Received);
            Assert.Equal(expectedChannel, notificationPublisher.Received.Channel);
            Assert.Equivalent(expectedNotification, notificationPublisher.Received.Notification);
        }
    }

    public class when_user_deleted
    {
        [Fact]
        public async Task publishes_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var notificationPublisher = new FakeNotificationPublisher();
            var messageStore = new FakeMessageStore();
            var expectedChannel = UserModule.StreamName(username);
            var expectedNotification = new Notifications.User(Operation.Delete, username, email);
            var userRegistered = new UserRegistered(username, email);
            var userDeleted = new UserDeleted(username);
            var context = MessageContext.From(userRegistered) with { StreamName = expectedChannel};
            messageStore.HasExistingMessages(expectedChannel, userRegistered, userDeleted);

            await UserNotificationPublisher.Handle(
                context,
                messageStore,
                notificationPublisher,
                CancellationToken.None
            );

            Assert.NotNull(notificationPublisher.Received);
            Assert.Equal(expectedChannel, notificationPublisher.Received.Channel);
            Assert.Equivalent(expectedNotification, notificationPublisher.Received.Notification);
        }
    }
}
