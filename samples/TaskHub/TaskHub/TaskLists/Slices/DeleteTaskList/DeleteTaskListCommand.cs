using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.DeleteTaskList;

public record DeleteTaskListCommand(Guid Id) : ICommand
{
    public string StreamName() => TaskListModule.StreamName(Id);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<object> Execute()
    {
        yield return new TaskListDeleted(Id);
    }
}
