using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserMentionNotification;

public record SendUserMentionNotificationCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public IEnumerable<object> Execute() => [new UserMentionNotificationSent(TaskListId, Task, Username)];
}
