using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class ChangeTaskListNameHandler : ICommandHandler<ChangeTaskListName>
{
    public IStreamName StreamName(ChangeTaskListName command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(ChangeTaskListName command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(ChangeTaskListName command)
    {
        yield return new TaskListNameChanged(command.Id, command.Name);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list name changed")
            .Given()
            .When(new ChangeTaskListName(Example.Guid, Example.String))
            .Then(new TaskListNameChanged(Example.Guid, Example.String))
    ];
}
