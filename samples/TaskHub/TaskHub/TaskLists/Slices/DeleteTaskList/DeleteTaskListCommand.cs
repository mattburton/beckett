using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.DeleteTaskList;

public record DeleteTaskListCommand(Guid Id) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListDeleted(Id);
    }
}
