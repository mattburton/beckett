using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.DeleteList;

public record DeleteListCommand(Guid Id) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListDeleted(Id);
    }
}
