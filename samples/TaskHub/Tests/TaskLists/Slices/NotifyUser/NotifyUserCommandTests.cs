using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.NotifyUser;

namespace Tests.TaskLists.Slices.NotifyUser;

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
