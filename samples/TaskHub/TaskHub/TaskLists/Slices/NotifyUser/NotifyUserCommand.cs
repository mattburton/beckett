using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.NotifyUser;

public record NotifyUserCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public string StreamName() => TaskListModule.StreamName(TaskListId);

    public IEnumerable<object> Execute()
    {
        yield return new UserNotificationSent(TaskListId, Task, Username);
    }
}
