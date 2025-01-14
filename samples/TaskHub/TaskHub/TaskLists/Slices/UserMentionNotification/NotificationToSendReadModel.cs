using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserMentionNotification;

[State]
public partial class NotificationToSendReadModel
{
    public Dictionary<string, bool> SentNotifications { get; init; } = [];

    public bool AlreadySentFor(string task) => SentNotifications.TryGetValue(task, out var sent) && sent;

    private void Apply(UserMentionedInTask e)
    {
        SentNotifications[e.Task] = false;
    }

    private void Apply(UserMentionNotificationSent e)
    {
        SentNotifications[e.Task] = true;
    }
}
