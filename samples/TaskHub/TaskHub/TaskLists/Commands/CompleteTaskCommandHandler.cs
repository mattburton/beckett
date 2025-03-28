using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public partial class CompleteTaskCommandHandler : ICommandHandler<CompleteTaskCommand, CompleteTaskCommandHandler.State>
{
    public IStreamName StreamName(CompleteTaskCommand command) => new TaskListStream(command.TaskListId);

    public IEnumerable<IEvent> Handle(CompleteTaskCommand command, State state)
    {
        if (state.CompletedItems.Contains(command.Task))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(command.TaskListId, command.Task);
    }

    [ReadModel]
    public partial class State
    {
        public HashSet<string> CompletedItems { get; } = [];

        private void Apply(TaskCompleted e) => CompletedItems.Add(e.Task);
    }
}
