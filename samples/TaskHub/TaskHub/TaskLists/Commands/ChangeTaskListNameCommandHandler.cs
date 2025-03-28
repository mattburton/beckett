using Contracts.TaskLists.Commands;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class ChangeTaskListNameCommandHandler : ICommandHandler<ChangeTaskListNameCommand>
{
    public IStreamName StreamName(ChangeTaskListNameCommand command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(ChangeTaskListNameCommand command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(ChangeTaskListNameCommand command)
    {
        yield return new TaskListNameChanged(command.Id, command.Name);
    }
}
