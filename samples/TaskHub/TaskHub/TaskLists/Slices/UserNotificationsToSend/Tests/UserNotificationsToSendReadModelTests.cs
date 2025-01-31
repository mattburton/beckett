using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserNotificationsToSend.Tests;

public class UserNotificationsToSendReadModelTests : StateSpecificationFixture<UserNotificationsToSendReadModel>
{
    [Fact]
    public void user_mentioned_in_task()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var username = Generate.String();

        Specification
            .Given(
                new UserMentionedInTask(taskListId, task, username)
            ).Then(
                new UserNotificationsToSendReadModel
                {
                    Notifications = new Dictionary<string, bool>
                    {
                        { task, false }
                    }
                }
            );
    }

    [Fact]
    public void user_mention_notification_sent()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var username = Generate.String();

        Specification
            .Given(
                new UserMentionedInTask(taskListId, task, username),
                new UserNotificationSent(taskListId, task, username)
            ).Then(
                new UserNotificationsToSendReadModel
                {
                    Notifications = new Dictionary<string, bool>
                    {
                        { task, true }
                    }
                }
            );
    }
}
