using Beckett.Commands;

namespace Taskmaster.TaskLists.CompleteTask;

public record CompleteTaskCommand(Guid Id, string Item) : ICommand<CompleteTaskCommand.DecisionState>
{
    public IEnumerable<object> Execute(DecisionState state)
    {
        if (state.CompletedItems.Contains(Item))
        {
            throw new TaskAlreadyCompletedException();
        }

        yield return new TaskCompleted(Id, Item);
    }

    public class DecisionState : IApply
    {
        public HashSet<string> CompletedItems { get; } = [];

        public void Apply(object message)
        {
            if (message is TaskCompleted e)
            {
                CompletedItems.Add(e.Item);
            }
        }
    }
}
