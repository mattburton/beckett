using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class DeleteTaskListHandler : ICommandHandler<DeleteTaskList>
{
    public IStreamName StreamName(DeleteTaskList command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(DeleteTaskList command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(DeleteTaskList command)
    {
        yield return new TaskListDeleted(command.Id);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list deleted")
            .Given()
            .When(new DeleteTaskList(Example.Guid))
            .Then(new TaskListDeleted(Example.Guid))
    ];
}
