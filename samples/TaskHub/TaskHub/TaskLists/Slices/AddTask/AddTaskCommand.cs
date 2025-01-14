using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.AddTask;

public partial record AddTaskCommand(Guid TaskListId, string Task) : ICommand<AddTaskCommand.State>
{
    public IEnumerable<object> Execute(State state)
    {
        if (state.Items.Contains(Task))
        {
            throw new TaskAlreadyAddedException();
        }

        yield return new TaskAdded(TaskListId, Task);

        var match = Matcher.Username().Match(Task);

        if (!match.Success)
        {
            yield break;
        }

        var username = match.Value.TrimStart('@');

        yield return new UserMentionedInTask(TaskListId, Task, username);
    }

    [State]
    public partial class State
    {
        public HashSet<string> Items { get; } = [];

        private void Apply(TaskAdded e) => Items.Add(e.Task);
    }
}

internal static partial class Matcher
{
    [GeneratedRegex(@"@(\w+)\b", RegexOptions.Compiled)]
    public static partial Regex Username();
}
