using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.DeleteList;

public record DeleteListCommand(Guid Id) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListDeleted(Id);
    }
}
