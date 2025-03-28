using Contracts.Users.Notifications;
using Core.Streams;
using TaskHub.Users.Events;
using TaskHub.Users.Processors.PublishNotification;
using TaskHub.Users.Streams;

namespace Tests.Users.Processors.PublishNotification;

public class PublishNotificationProcessorTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var reader = new FakeStreamReader();
            var processor = new PublishNotificationProcessor(reader);
            var stream = new UserStream(username);
            var expectedNotification = new UserCreatedNotification(username, email);
            var userRegisteredEvent = new UserRegistered(username, email);
            var context = MessageContext.From(userRegisteredEvent) with { StreamName = stream.StreamName() };
            reader.HasExistingStream(stream, userRegisteredEvent);

            var result = await processor.Handle(context, CancellationToken.None);

            result.NotificationPublished(expectedNotification);
        }
    }

    public class when_user_deleted
    {
        [Fact]
        public async Task publishes_notification()
        {
            var username = Generate.String();
            var email = Generate.String();
            var reader = new FakeStreamReader();
            var processor = new PublishNotificationProcessor(reader);
            var stream = new UserStream(username);
            var expectedNotification = new UserDeletedNotification(username);
            var userRegisteredEvent = new UserRegistered(username, email);
            var userDeletedEvent = new UserDeleted(username);
            var context = MessageContext.From(userRegisteredEvent) with { StreamName = stream.StreamName() };
            reader.HasExistingStream(stream, userRegisteredEvent, userDeletedEvent);

            var result = await processor.Handle(context, CancellationToken.None);

            result.NotificationPublished(expectedNotification);
        }
    }
}
