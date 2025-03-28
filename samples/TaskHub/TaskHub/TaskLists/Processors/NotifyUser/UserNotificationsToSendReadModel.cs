using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Processors.NotifyUser;

[ReadModel]
public partial class UserNotificationsToSendReadModel
{
    public Dictionary<string, bool> Notifications { get; init; } = [];

    public bool AlreadySentFor(string task) => Notifications.TryGetValue(task, out var sent) && sent;

    private void Apply(UserMentionedInTask e)
    {
        Notifications[e.Task] = false;
    }

    private void Apply(UserNotificationSent e)
    {
        Notifications[e.Task] = true;
    }
}
