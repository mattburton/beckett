using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserMentionNotification.Tests;

public class NotificationToSendReadModelTests : StateSpecificationFixture<NotificationToSendReadModel>
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
                new NotificationToSendReadModel
                {
                    SentNotifications = new Dictionary<string, bool>
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
                new UserMentionNotificationSent(taskListId, task, username)
            ).Then(
                new NotificationToSendReadModel
                {
                    SentNotifications = new Dictionary<string, bool>
                    {
                        { task, true }
                    }
                }
            );
    }
}
