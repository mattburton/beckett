using Beckett.Commands;

namespace Taskmaster.TaskLists.CreateList;

public record CreateListCommand(Guid Id, string Name) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new TaskListCreated(Id, Name);
    }
}
