using TaskLists.Events;

namespace TaskLists.CompleteTask;

public partial record CompleteTaskCommand(Guid TaskListId, string Task) : ICommand<CompleteTaskCommand.State>
{
    public IStreamName StreamName() => new TaskListStream(TaskListId);

    public IEnumerable<IInternalEvent> Execute(State state)
    {
        if (state.CompletedItems.Contains(Task))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(TaskListId, Task);
    }

    [State]
    public partial class State
    {
        public HashSet<string> CompletedItems { get; } = [];

        private void Apply(TaskCompleted e) => CompletedItems.Add(e.Task);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("task completed")
            .Given()
            .When(new CompleteTaskCommand(Example.Guid, Example.String))
            .Then(new TaskCompleted(Example.Guid, Example.String)),
        new Scenario("error when task is already completed")
            .Given(new TaskCompleted(Example.Guid, Example.String))
            .When(new CompleteTaskCommand(Example.Guid, Example.String))
            .Throws<TaskAlreadyCompletedException>()
    ];
}
