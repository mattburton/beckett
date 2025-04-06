using TaskLists.Events;

namespace TaskLists.AddList;

public record AddListCommand(Guid Id, string Name) : ICommand
{
    public IStreamName StreamName() => new TaskListStream(Id);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new TaskListAdded(Id, Name);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list is added")
            .Given()
            .When(new AddListCommand(Example.Guid, Example.String))
            .Then(new TaskListAdded(Example.Guid, Example.String))
    ];
}
