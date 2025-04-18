using Users.Contracts;
using Users.Events;
using Users.PublishEvent;

namespace Users.Tests.PublishEvent;

public class PublishEventProcessorTests
{
    public class when_user_registered
    {
        [Fact]
        public async Task publishes_external_event()
        {
            var reader = new FakeStreamReader();
            var processor = new PublishEventProcessor(reader);
            var stream = new UserStream(Example.String);
            var expectedExternalEvent = new ExternalUserCreated(Example.String, Example.String);
            var userRegisteredEvent = new UserRegistered(Example.String, Example.String);
            var context = MessageContext.From(userRegisteredEvent) with { StreamName = stream.StreamName() };
            reader.HasExistingStream(stream, userRegisteredEvent);

            var result = await processor.Handle([context], CancellationToken.None);

            result.ExternalEventPublished(expectedExternalEvent);
        }
    }

    public class when_user_deleted
    {
        [Fact]
        public async Task publishes_external_event()
        {
            var reader = new FakeStreamReader();
            var processor = new PublishEventProcessor(reader);
            var stream = new UserStream(Example.String);
            var expectedExternalEvent = new ExternalUserDeleted(Example.String);
            var userRegisteredEvent = new UserRegistered(Example.String, Example.String);
            var userDeletedEvent = new UserDeleted(Example.String);
            var context = MessageContext.From(userRegisteredEvent) with { StreamName = stream.StreamName() };
            reader.HasExistingStream(stream, userRegisteredEvent, userDeletedEvent);

            var result = await processor.Handle([context], CancellationToken.None);

            result.ExternalEventPublished(expectedExternalEvent);
        }
    }
}
