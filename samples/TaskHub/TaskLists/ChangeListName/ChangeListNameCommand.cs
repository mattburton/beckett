using TaskLists.Events;

namespace TaskLists.ChangeListName;

public record ChangeListNameCommand(Guid Id, string Name) : ICommand
{
    public IStreamName StreamName() => new TaskListStream(Id);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new TaskListNameChanged(Id, Name);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list name changed")
            .Given()
            .When(new ChangeListNameCommand(Example.Guid, Example.String))
            .Then(new TaskListNameChanged(Example.Guid, Example.String))
    ];
}
