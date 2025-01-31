using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.ChangeTaskListName;

public record ChangeTaskListNameCommand(Guid Id, string Name) : ICommand
{
    public string StreamName() => TaskListModule.StreamName(Id);

    public IEnumerable<object> Execute()
    {
        yield return new TaskListNameChanged(Id, Name);
    }
}
