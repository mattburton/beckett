using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.SendUserNotification.Tests;

public class SendUserNotificationCommandTests : CommandSpecificationFixture<SendUserNotificationCommand>
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
