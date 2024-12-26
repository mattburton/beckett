using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.ChangeListName;

public record ChangeListNameCommand(Guid Id, string Name) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListNameChanged(Id, Name);
    }
}
