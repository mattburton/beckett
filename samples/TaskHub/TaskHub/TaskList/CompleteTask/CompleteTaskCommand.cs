using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.CompleteTask;

public record CompleteTaskCommand(Guid TaskListId, string Task) : ICommand<CompleteTaskCommand.State>
{
    public IEnumerable<object> Execute(State state)
    {
        if (state.CompletedItems.Contains(Task))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(TaskListId, Task);
    }

    public class State : IApply
    {
        public HashSet<string> CompletedItems { get; } = [];

        public void Apply(object message)
        {
            if (message is TaskCompleted e)
            {
                CompletedItems.Add(e.Task);
            }
        }
    }
}
