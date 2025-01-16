using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.UserAddedNotification.Tests;

public class UserAddedNotificationPublisherTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_user_added_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var expectedStreamName = UserModule.NotificationStreamName(username);
            var messageStore = new FakeMessageStore();
            var userRegistered = new UserRegistered(username, email);

            await UserAddedNotificationPublisher.UserRegistered(userRegistered, messageStore, CancellationToken.None);

            var message = Assert.IsType<Contracts.Notifications.UserAddedNotification>(
                messageStore.LatestMessage(expectedStreamName)
            );
            Assert.Equal(username, message.Username);
            Assert.Equal(email, message.Email);
        }
    }
}
