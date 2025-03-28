using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Processors.NotifyUser;

namespace Tests.TaskLists.Processors.NotifyUser;

public class SendUserNotificationCommandTests :
    CommandHandlerFixture<SendUserNotificationCommand, SendUserNotificationCommand.Handler>
{
    [Fact]
    public void user_notification_sent()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var username = Generate.String();

        Specification
            .When(
                new SendUserNotificationCommand(taskListId, task, username)
            )
            .Then(
                new UserNotificationSent(taskListId, task, username)
            );
    }
}
