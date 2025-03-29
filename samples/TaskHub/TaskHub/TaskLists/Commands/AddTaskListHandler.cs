using Contracts.TaskLists.Commands;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public class AddTaskListHandler : ICommandHandler<AddTaskList>, IHaveScenarios
{
    public IStreamName StreamName(AddTaskList command) => new TaskListStream(command.Id);

    public ExpectedVersion StreamVersion(AddTaskList command) => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IEvent> Handle(AddTaskList command)
    {
        yield return new TaskListAdded(command.Id, command.Name);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list is added")
            .Given()
            .When(new AddTaskList(Example.Guid, Example.String))
            .Then(new TaskListAdded(Example.Guid, Example.String))
    ];
}
