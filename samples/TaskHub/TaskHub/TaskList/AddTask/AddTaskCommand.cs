using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.AddTask;

public record AddTaskCommand(Guid TaskListId, string Task) : ICommand<AddTaskCommand.State>
{
    public IEnumerable<object> Execute(State state)
    {
        if (state.Items.Contains(Task))
        {
            throw new TaskAlreadyAddedException();
        }

        yield return new TaskAdded(TaskListId, Task);
    }

    public class State : IApply
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

internal static partial class Matcher
{
    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    public static partial Regex Username();
}
