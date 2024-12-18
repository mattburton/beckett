using Beckett.Commands;

namespace Taskmaster.TaskLists.AddTask;

public record AddTaskCommand(Guid Id, string Task) : ICommand<AddTaskCommand.DecisionState>
{
    public IEnumerable<object> Execute(DecisionState state)
    {
        throw new NotImplementedException();
    }

    public async Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)


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
