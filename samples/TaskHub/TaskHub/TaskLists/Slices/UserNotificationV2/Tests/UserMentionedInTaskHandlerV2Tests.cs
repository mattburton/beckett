using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.UserLookup;
using TaskHub.TaskLists.Slices.UserNotificationsToSend;
using TaskHub.Users.Contracts.Notifications;

namespace TaskHub.TaskLists.Slices.UserNotificationV2.Tests;

public class UserMentionedInTaskHandlerV2Tests
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
                var task = Generate.Extensions.Task();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(new UserAddedNotification(username, email));
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandlerV2.Handle(
                    message,
                    queryDispatcher,
                    emailService,
                    commandExecutor,
                    CancellationToken.None
                );

                Assert.NotNull(emailService.SentEmail);
                Assert.Equal(email, emailService.SentEmail.To);
                Assert.Contains(task, emailService.SentEmail.Body);
            }

            [Fact]
            public async Task executes_command()
            {
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.Extensions.Task();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(new UserAddedNotification(username, email));
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var expectedStreamName = TaskListModule.StreamName(taskListId);
                var expectedCommand = new SendUserMentionNotificationCommand(taskListId, task, username);
                var message = new UserMentionedInTask(taskListId, task, username);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandlerV2.Handle(
                    message,
                    queryDispatcher,
                    emailService,
                    commandExecutor,
                    CancellationToken.None
                );

                Assert.NotNull(commandExecutor.Received);
                Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
                Assert.Equal(expectedCommand, commandExecutor.Received.Command);
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
                var task = Generate.Extensions.Task();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(new UserAddedNotification(username, email));
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserMentionNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandlerV2.Handle(
                    message,
                    queryDispatcher,
                    emailService,
                    commandExecutor,
                    CancellationToken.None
                );

                Assert.Null(emailService.SentEmail);
            }

            [Fact]
            public async Task does_not_execute_command()
            {
                var taskListId = Generate.Guid();
                var username = Generate.String();
                var email = Generate.String();
                var task = Generate.Extensions.Task();
                var emailService = new FakeEmailService();
                var queryDispatcher = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(new UserAddedNotification(username, email));
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserMentionNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryDispatcher.Returns(userLookupQuery, userReadModel);
                queryDispatcher.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandlerV2.Handle(
                    message,
                    queryDispatcher,
                    emailService,
                    commandExecutor,
                    CancellationToken.None
                );

                Assert.Null(commandExecutor.Received);
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
            var commandExecutor = new FakeCommandExecutor();
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());

            await UserMentionedInTaskHandlerV2.Handle(
                message,
                queryDispatcher,
                emailService,
                commandExecutor,
                CancellationToken.None
            );

            Assert.Null(emailService.SentEmail);
        }

        [Fact]
        public async Task does_not_execute_command()
        {
            var emailService = new FakeEmailService();
            var queryExecutor = new FakeQueryDispatcher();
            var commandExecutor = new FakeCommandExecutor();
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());

            await UserMentionedInTaskHandlerV2.Handle(
                message,
                queryExecutor,
                emailService,
                commandExecutor,
                CancellationToken.None
            );

            Assert.Null(commandExecutor.Received);
        }
    }
}

public static class GenerateMethods
{
    public static string Task(this Generate _) => Generate.String() + " [V2]";
}
