using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.SendUserNotification;

public record SendUserNotificationCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public string StreamName() => TaskListModule.StreamName(TaskListId);

    public IEnumerable<object> Execute()
    {
        yield return new UserNotificationSent(TaskListId, Task, Username);
    }
}
