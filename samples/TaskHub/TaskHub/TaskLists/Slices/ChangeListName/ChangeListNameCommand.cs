using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.ChangeListName;

public record ChangeListNameCommand(Guid Id, string Name) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListNameChanged(Id, Name);
    }
}