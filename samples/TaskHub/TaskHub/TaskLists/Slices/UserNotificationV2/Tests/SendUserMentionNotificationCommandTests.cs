using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserNotificationV2.Tests;

public class SendUserMentionNotificationCommandTests : CommandSpecificationFixture<SendUserMentionNotificationCommand>
{
    [Fact]
    public void user_mention_notification_sent()
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
