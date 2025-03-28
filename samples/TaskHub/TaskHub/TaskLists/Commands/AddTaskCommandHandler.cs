using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public partial class AddTaskCommandHandler : ICommandHandler<AddTaskCommand, AddTaskCommandHandler.State>
{
    public IStreamName StreamName(AddTaskCommand command) => new TaskListStream(command.TaskListId);

    public IEnumerable<IEvent> Handle(AddTaskCommand command, State state)
    {
        if (state.Items.Contains(command.Task))
        {
            throw new TaskAlreadyAddedException();
        }

        yield return new TaskAdded(command.TaskListId, command.Task);

        var match = Matcher.Username().Match(command.Task);

        if (!match.Success)
        {
            yield break;
        }

        var username = match.Value.TrimStart('@');

        yield return new UserMentionedInTask(command.TaskListId, command.Task, username);
    }

    [ReadModel]
    public partial class State
    {
        public HashSet<string> Items { get; } = [];

        private void Apply(TaskAdded e) => Items.Add(e.Task);
    }

    internal static partial class Matcher
    {
        [GeneratedRegex(@"@(\w+)\b")]
        public static partial Regex Username();
    }
}
