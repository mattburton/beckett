using TaskLists.Events;

namespace TaskLists.DeleteList;

public record DeleteListCommand(Guid Id) : ICommand
{
    public IStreamName StreamName() => new TaskListStream(Id);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new TaskListDeleted(Id);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task list deleted")
            .Given()
            .When(new DeleteListCommand(Example.Guid))
            .Then(new TaskListDeleted(Example.Guid))
    ];
}
