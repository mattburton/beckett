using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.CompleteTask;

public partial record CompleteTaskCommand(Guid TaskListId, string Task) : ICommand<CompleteTaskCommand.State>
{
    public IEnumerable<object> Execute(State state)
    {
        if (state.CompletedItems.Contains(Task))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(TaskListId, Task);
    }

    [ReadModel]
    public partial class State
    {
        public HashSet<string> CompletedItems { get; } = [];

        private void Apply(TaskCompleted e)
        {
            CompletedItems.Add(e.Task);
        }
    }
}
