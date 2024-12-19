using Beckett.Commands;

namespace Taskmaster.TaskLists.AddTask;

public record AddTaskCommand(Guid Id, string Task) : ICommand<AddTaskCommand.DecisionState>
{
    public IEnumerable<object> Execute(DecisionState state)
    {
        if (state.Items.Contains(Task))
        {
            throw new TaskAlreadyAddedException();
        }

        yield return new Message(new TaskAdded(Id, Task)).WithCorrelationId(Guid.NewGuid().ToString());
    }

    public class DecisionState : IApply
    {
        public HashSet<string> Items { get; } = [];

        public void Apply(object message)
        {
            switch (message)
            {
                case TaskAdded e:
                    Apply(e);
                    break;
            }
        }

        private void Apply(TaskAdded e) => Items.Add(e.Task);
    }
}
