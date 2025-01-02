using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.DeleteList;

public record DeleteListCommand(Guid Id) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListDeleted(Id);
    }
}
