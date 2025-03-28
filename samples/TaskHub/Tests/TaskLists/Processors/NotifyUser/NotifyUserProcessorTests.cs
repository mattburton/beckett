using Contracts.Users.Notifications;
using TaskHub.Infrastructure.Email;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Processors.NotifyUser;

namespace Tests.TaskLists.Processors.NotifyUser;

public class NotifyUserProcessorTests
{
    public class when_user_exists
    {
        public class when_notification_has_not_been_sent
        {
            [Fact]
            public async Task sends_email()
            {
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var processor = new NotifyUserProcessor(queryDispatcher, emailService);
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = ReadModelBuilder.Build<UserLookupReadModel>(
                    new UserCreatedNotification(username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = ReadModelBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                var context = MessageContext<UserMentionedInTask>.From(message);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await processor.Handle(context, CancellationToken.None);

                Assert.NotNull(emailService.Received);
                Assert.Equal(email, emailService.Received.To);
                Assert.Contains(task, emailService.Received.Body);
            }

            [Fact]
            public async Task executes_command()
            {
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var processor = new NotifyUserProcessor(queryDispatcher, emailService);
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = ReadModelBuilder.Build<UserLookupReadModel>(
                    new UserCreatedNotification(username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = ReadModelBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var expectedCommand = new SendUserNotificationCommand(taskListId, task, username);
                var message = new UserMentionedInTask(taskListId, task, username);
                var context = MessageContext<UserMentionedInTask>.From(message);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

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
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var processor = new NotifyUserProcessor(queryDispatcher, emailService);
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = ReadModelBuilder.Build<UserLookupReadModel>(
                    new UserCreatedNotification(username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = ReadModelBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                var context = MessageContext<UserMentionedInTask>.From(message);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await processor.Handle(context, CancellationToken.None);

                Assert.Null(emailService.Received);
            }

            [Fact]
            public async Task does_not_execute_command()
            {
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var processor = new NotifyUserProcessor(queryDispatcher, emailService);
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = ReadModelBuilder.Build<UserLookupReadModel>(
                    new UserCreatedNotification(username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = ReadModelBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                var context = MessageContext<UserMentionedInTask>.From(message);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

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
            var queryDispatcher = new FakeQueryDispatcher();
            var processor = new NotifyUserProcessor(queryDispatcher, emailService);
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());
            var context = MessageContext<UserMentionedInTask>.From(message);

            await processor.Handle(context, CancellationToken.None);

            Assert.Null(emailService.Received);
        }

        [Fact]
        public async Task does_not_execute_command()
        {
            var emailService = new FakeEmailService();
            var queryDispatcher = new FakeQueryDispatcher();
            var processor = new NotifyUserProcessor(queryDispatcher, emailService);
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());
            var context = MessageContext<UserMentionedInTask>.From(message);

            var result = await processor.Handle(context, CancellationToken.None);

            Assert.Null(result.Command);
        }
    }
}
