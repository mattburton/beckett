using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.AddTask;

public partial record AddTaskCommand(Guid TaskListId, string Task) : ICommand<AddTaskCommand.State>
{
    public IEnumerable<object> Execute(State state)
    {
        if (state.Items.Contains(Task))
        {
            throw new TaskAlreadyAddedException();
        }

        yield return new TaskAdded(TaskListId, Task);
    }

    [ReadModel]
    public partial class State
    {
        public HashSet<string> Items { get; } = [];

        private void Apply(TaskAdded e) => Items.Add(e.Task);

    }
}

internal static partial class Matcher
{
    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    public static partial Regex Username();
}
