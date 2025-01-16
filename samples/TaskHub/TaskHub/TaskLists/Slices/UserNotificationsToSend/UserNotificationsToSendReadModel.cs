using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserNotificationsToSend;

[State]
public partial class UserNotificationsToSendReadModel
{
    public Dictionary<string, bool> Notifications { get; init; } = [];

    public bool AlreadySentFor(string task) => Notifications.TryGetValue(task, out var sent) && sent;

    private void Apply(UserMentionedInTask e)
    {
        Notifications[e.Task] = false;
    }

    private void Apply(UserMentionNotificationSent e)
    {
        Notifications[e.Task] = true;
    }
}