using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.NotifyUser.Tests;

public class NotifyUserCommandTests : CommandSpecificationFixture<NotifyUserCommand>
{
    [Fact]
    public void user_notification_sent()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var username = Generate.String();

        Specification
            .When(
                new NotifyUserCommand(taskListId, task, username)
            )
            .Then(
                new UserNotificationSent(taskListId, task, username)
            );
    }
}
