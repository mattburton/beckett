using Infrastructure.Email;
using TaskLists.Events;
using TaskLists.NotifyUser;
using Users.Contracts;

namespace TaskLists.Tests.NotifyUser;

public class NotifyUserProcessorTests
{
    public class when_user_exists
    {
        public class when_notification_has_not_been_sent
        {
            [Fact]
            public async Task sends_email()
            {
                var emailService = new FakeEmailService();
                var module = new FakeTaskListModule();
                var processor = new NotifyUserProcessor(module, emailService);
                var userLookupQuery = new UserLookupQuery(Example.String);
                var user = StateBuilder.Build<UserLookupReadModel>(
                    new ExternalUserCreated(Example.String, Example.String)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(Example.Guid);
                var userNotificationsToSend = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(Example.Guid, Example.String, Example.String)
                );
                var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
                var context = MessageContext<UserMentionedInTask>.From(message);
                module.Returns(userLookupQuery, user);
                module.Returns(userNotificationsToSendQuery, userNotificationsToSend);

                await processor.Handle(context, CancellationToken.None);

                Assert.NotNull(emailService.Received);
                Assert.Equal(Example.String, emailService.Received.To);
                Assert.Contains(Example.String, emailService.Received.Body);
            }

            [Fact]
            public async Task executes_command()
            {
                var emailService = new FakeEmailService();
                var module = new FakeTaskListModule();
                var processor = new NotifyUserProcessor(module, emailService);
                var userLookupQuery = new UserLookupQuery(Example.String);
                var user = StateBuilder.Build<UserLookupReadModel>(
                    new ExternalUserCreated(Example.String, Example.String)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(Example.Guid);
                var userNotificationsToSend = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(Example.Guid, Example.String, Example.String)
                );
                var expectedCommand = new SendUserNotificationCommand(Example.Guid, Example.String, Example.String);
                var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
                var context = MessageContext<UserMentionedInTask>.From(message);
                module.Returns(userLookupQuery, user);
                module.Returns(userNotificationsToSendQuery, userNotificationsToSend);

                var result = await processor.Handle(context, CancellationToken.None);

                Assert.NotNull(result.Command);
                Assert.Equivalent(expectedCommand, result.Command);
            }
        }

        public class when_notification_has_been_sent
        {
            [Fact]
            public async Task does_not_send_email()
            {
                var emailService = new FakeEmailService();
                var module = new FakeTaskListModule();
                var processor = new NotifyUserProcessor(module, emailService);
                var userLookupQuery = new UserLookupQuery(Example.String);
                var user = StateBuilder.Build<UserLookupReadModel>(
                    new ExternalUserCreated(Example.String, Example.String)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(Example.Guid);
                var userNotificationsToSend = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(Example.Guid, Example.String, Example.String),
                    new UserNotificationSent(Example.Guid, Example.String, Example.String)
                );
                var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
                var context = MessageContext<UserMentionedInTask>.From(message);
                module.Returns(userLookupQuery, user);
                module.Returns(userNotificationsToSendQuery, userNotificationsToSend);

                await processor.Handle(context, CancellationToken.None);

                Assert.Null(emailService.Received);
            }

            [Fact]
            public async Task does_not_execute_command()
            {
                var emailService = new FakeEmailService();
                var module = new FakeTaskListModule();
                var processor = new NotifyUserProcessor(module, emailService);
                var userLookupQuery = new UserLookupQuery(Example.String);
                var user = StateBuilder.Build<UserLookupReadModel>(
                    new ExternalUserCreated(Example.String, Example.String)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(Example.Guid);
                var userNotificationsToSend = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(Example.Guid, Example.String, Example.String),
                    new UserNotificationSent(Example.Guid, Example.String, Example.String)
                );
                var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
                var context = MessageContext<UserMentionedInTask>.From(message);
                module.Returns(userLookupQuery, user);
                module.Returns(userNotificationsToSendQuery, userNotificationsToSend);

                var result = await processor.Handle(context, CancellationToken.None);

                Assert.Null(result.Command);
            }
        }
    }

    public class when_user_does_not_exist
    {
        [Fact]
        public async Task does_not_send_email()
        {
            var emailService = new FakeEmailService();
            var module = new FakeTaskListModule();
            var processor = new NotifyUserProcessor(module, emailService);
            var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
            var context = MessageContext<UserMentionedInTask>.From(message);

            await processor.Handle(context, CancellationToken.None);

            Assert.Null(emailService.Received);
        }

        [Fact]
        public async Task does_not_execute_command()
        {
            var emailService = new FakeEmailService();
            var module = new FakeTaskListModule();
            var processor = new NotifyUserProcessor(module, emailService);
            var message = new UserMentionedInTask(Example.Guid, Example.String, Example.String);
            var context = MessageContext<UserMentionedInTask>.From(message);

            var result = await processor.Handle(context, CancellationToken.None);

            Assert.Null(result.Command);
        }
    }
}
