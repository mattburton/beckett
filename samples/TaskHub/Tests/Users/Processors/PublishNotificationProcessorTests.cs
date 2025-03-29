using Contracts.Users.Notifications;
using Core.Streams;
using TaskHub.Users.Events;
using TaskHub.Users.Processors;
using TaskHub.Users.Streams;

namespace Tests.Users.Processors;

public class PublishNotificationProcessorTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_notification()
        {
            var reader = new FakeStreamReader();
            var processor = new PublishNotificationProcessor(reader);
            var stream = new UserStream(Example.String);
            var expectedNotification = new UserCreatedNotification(Example.String, Example.String);
            var userRegisteredEvent = new UserRegistered(Example.String, Example.String);
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
            var reader = new FakeStreamReader();
            var processor = new PublishNotificationProcessor(reader);
            var stream = new UserStream(Example.String);
            var expectedNotification = new UserDeletedNotification(Example.String);
            var userRegisteredEvent = new UserRegistered(Example.String, Example.String);
            var userDeletedEvent = new UserDeleted(Example.String);
            var context = MessageContext.From(userRegisteredEvent) with { StreamName = stream.StreamName() };
            reader.HasExistingStream(stream, userRegisteredEvent, userDeletedEvent);

            var result = await processor.Handle(context, CancellationToken.None);

            result.NotificationPublished(expectedNotification);
        }
    }
}
