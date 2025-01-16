using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.UserNotificationsToSend;
using TaskHub.Users.Contracts.Queries;
using TaskHub.Users.Events;
using TaskHub.Users.Slices.GetUser;

namespace TaskHub.TaskLists.Slices.UserNotificationV1.Tests;

public class UserMentionedInTaskHandlerV1Tests
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
                var queryExecutor = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var getUserQuery = new GetUserQuery(username);
                var userReadModel = StateBuilder.Build<GetUserReadModel>(new UserRegistered(username, email));
                var notificationToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var notificationToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryExecutor.Returns(getUserQuery, userReadModel);
                queryExecutor.Returns(notificationToSendQuery, notificationToSendReadModel);

                await UserMentionedInTaskHandlerV1.Handle(
                    message,
                    queryExecutor,
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
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryExecutor = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var getUserQuery = new GetUserQuery(username);
                var userReadModel = StateBuilder.Build<GetUserReadModel>(new UserRegistered(username, email));
                var notificationToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var notificationToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var expectedStreamName = TaskListModule.StreamName(taskListId);
                var expectedCommand = new SendUserMentionNotificationCommand(taskListId, task, username);
                var message = new UserMentionedInTask(taskListId, task, username);
                queryExecutor.Returns(getUserQuery, userReadModel);
                queryExecutor.Returns(notificationToSendQuery, notificationToSendReadModel);

                await UserMentionedInTaskHandlerV1.Handle(
                    message,
                    queryExecutor,
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
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryExecutor = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var getUserQuery = new GetUserQuery(username);
                var userReadModel = StateBuilder.Build<GetUserReadModel>(new UserRegistered(username, email));
                var notificationToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var notificationToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserMentionNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryExecutor.Returns(getUserQuery, userReadModel);
                queryExecutor.Returns(notificationToSendQuery, notificationToSendReadModel);

                await UserMentionedInTaskHandlerV1.Handle(
                    message,
                    queryExecutor,
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
                var task = Generate.String();
                var emailService = new FakeEmailService();
                var queryExecutor = new FakeQueryDispatcher();
                var commandExecutor = new FakeCommandExecutor();
                var getUserQuery = new GetUserQuery(username);
                var userReadModel = StateBuilder.Build<GetUserReadModel>(new UserRegistered(username, email));
                var notificationToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var notificationToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserMentionNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryExecutor.Returns(getUserQuery, userReadModel);
                queryExecutor.Returns(notificationToSendQuery, notificationToSendReadModel);

                await UserMentionedInTaskHandlerV1.Handle(
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

    public class when_user_does_not_exist
    {
        [Fact]
        public async Task does_not_send_email()
        {
            var emailService = new FakeEmailService();
            var queryExecutor = new FakeQueryDispatcher();
            var commandExecutor = new FakeCommandExecutor();
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());

            await UserMentionedInTaskHandlerV1.Handle(
                message,
                queryExecutor,
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

            await UserMentionedInTaskHandlerV1.Handle(
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
