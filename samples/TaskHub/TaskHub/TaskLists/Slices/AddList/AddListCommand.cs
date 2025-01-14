using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.AddList;

public record AddListCommand(Guid Id, string Name) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListAdded(Id, Name);
    }
}
