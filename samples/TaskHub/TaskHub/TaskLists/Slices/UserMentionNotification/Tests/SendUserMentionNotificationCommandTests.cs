using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserMentionNotification.Tests;

public class SendUserMentionNotificationCommandTests : CommandSpecificationFixture<SendUserMentionNotificationCommand>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var username = Generate.String();

        Specification
            .When(
                new SendUserMentionNotificationCommand(taskListId, task, username)
            )
            .Then(
                new UserMentionNotificationSent(taskListId, task, username)
            );
    }
}
