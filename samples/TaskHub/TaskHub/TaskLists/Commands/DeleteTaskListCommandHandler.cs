using Contracts.TaskLists.Commands;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class DeleteTaskListCommandHandler : ICommandHandler<DeleteTaskListCommand>
{
    public IStreamName StreamName(DeleteTaskListCommand command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(DeleteTaskListCommand command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(DeleteTaskListCommand command)
    {
        yield return new TaskListDeleted(command.Id);
    }
}
