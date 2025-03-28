using Contracts.TaskLists.Commands;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class AddTaskListCommandHandler : ICommandHandler<AddTaskListCommand>
{
    public IStreamName StreamName(AddTaskListCommand command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(AddTaskListCommand command) => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IEvent> Handle(AddTaskListCommand command)
    {
        yield return new TaskListAdded(command.Id, command.Name);
    }
}
