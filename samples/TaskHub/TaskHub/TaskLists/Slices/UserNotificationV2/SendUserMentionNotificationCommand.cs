using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserNotificationV2;

public record SendUserMentionNotificationCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new UserMentionNotificationSent(TaskListId, Task, Username);
    }
}
