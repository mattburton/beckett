using TaskHub.TaskLists;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.NotifyUser;
using TaskHub.TaskLists.Slices.UserLookup;
using TaskHub.TaskLists.Slices.UserNotificationsToSend;

namespace Tests.TaskLists.Slices.NotifyUser;

public class UserMentionedInTaskHandlerTests
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
                var queryBus = new FakeQueryBus();
                var commandBus = new FakeCommandBus();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(
                    new TaskHub.Users.Notifications.UserNotification(Operation.Create, username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryBus.Returns(userLookupQuery, userReadModel);
                queryBus.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandler.Handle(
                    message,
                    queryBus,
                    emailService,
                    commandBus,
                    CancellationToken.None
                );

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
                var queryBus = new FakeQueryBus();
                var commandBus = new FakeCommandBus();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(
                    new TaskHub.Users.Notifications.UserNotification(Operation.Create, username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username)
                );
                var expectedStreamName = TaskListModule.StreamName(taskListId);
                var expectedCommand = new NotifyUserCommand(taskListId, task, username);
                var message = new UserMentionedInTask(taskListId, task, username);
                queryBus.Returns(userLookupQuery, userReadModel);
                queryBus.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandler.Handle(
                    message,
                    queryBus,
                    emailService,
                    commandBus,
                    CancellationToken.None
                );

                var actualCommand = Assert.IsType<NotifyUserCommand>(commandBus.Received);
                Assert.Equal(expectedStreamName, actualCommand.StreamName());
                Assert.Equal(expectedCommand, actualCommand);
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
                var queryBus = new FakeQueryBus();
                var commandBus = new FakeCommandBus();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(
                    new TaskHub.Users.Notifications.UserNotification(Operation.Create, username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryBus.Returns(userLookupQuery, userReadModel);
                queryBus.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandler.Handle(
                    message,
                    queryBus,
                    emailService,
                    commandBus,
                    CancellationToken.None
                );

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
                var queryBus = new FakeQueryBus();
                var commandBus = new FakeCommandBus();
                var userLookupQuery = new UserLookupQuery(username);
                var userReadModel = StateBuilder.Build<UserLookupReadModel>(
                    new TaskHub.Users.Notifications.UserNotification(Operation.Create, username, email)
                );
                var userNotificationsToSendQuery = new UserNotificationsToSendQuery(taskListId);
                var userNotificationsToSendReadModel = StateBuilder.Build<UserNotificationsToSendReadModel>(
                    new UserMentionedInTask(taskListId, task, username),
                    new UserNotificationSent(taskListId, task, username)
                );
                var message = new UserMentionedInTask(taskListId, task, username);
                queryBus.Returns(userLookupQuery, userReadModel);
                queryBus.Returns(userNotificationsToSendQuery, userNotificationsToSendReadModel);

                await UserMentionedInTaskHandler.Handle(
                    message,
                    queryBus,
                    emailService,
                    commandBus,
                    CancellationToken.None
                );

                Assert.Null(commandBus.Received);
            }
        }
    }

    public class when_user_does_not_exist
    {
        [Fact]
        public async Task does_not_send_email()
        {
            var emailService = new FakeEmailService();
            var queryBus = new FakeQueryBus();
            var commandBus = new FakeCommandBus();
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());

            await UserMentionedInTaskHandler.Handle(
                message,
                queryBus,
                emailService,
                commandBus,
                CancellationToken.None
            );

            Assert.Null(emailService.Received);
        }

        [Fact]
        public async Task does_not_execute_command()
        {
            var emailService = new FakeEmailService();
            var queryExecutor = new FakeQueryBus();
            var commandBus = new FakeCommandBus();
            var message = new UserMentionedInTask(Generate.Guid(), Generate.String(), Generate.String());

            await UserMentionedInTaskHandler.Handle(
                message,
                queryExecutor,
                emailService,
                commandBus,
                CancellationToken.None
            );

            Assert.Null(commandBus.Received);
        }
    }
}
