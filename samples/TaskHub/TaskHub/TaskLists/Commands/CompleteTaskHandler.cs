using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public partial class CompleteTaskHandler : ICommandHandler<CompleteTask, CompleteTaskHandler.State>
{
    public IStreamName StreamName(CompleteTask command) => new TaskListStream(command.TaskListId);

    public IEnumerable<IEvent> Handle(CompleteTask command, State state)
    {
        if (state.CompletedItems.Contains(command.Task))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(command.TaskListId, command.Task);
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
            .When(new CompleteTask(Example.Guid, Example.String))
            .Then(new TaskCompleted(Example.Guid, Example.String)),
        new Scenario("error when task is already completed")
            .Given(new TaskCompleted(Example.Guid, Example.String))
            .When(new CompleteTask(Example.Guid, Example.String))
            .Throws<TaskAlreadyCompletedException>()
    ];
}
