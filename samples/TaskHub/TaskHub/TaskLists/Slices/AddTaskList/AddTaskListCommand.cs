using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.AddTaskList;

public record AddTaskListCommand(Guid Id, string Name) : ICommand
{
    public string StreamName() => TaskListModule.StreamName(Id);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<object> Execute()
    {
        yield return new TaskListAdded(Id, Name);
    }
}
