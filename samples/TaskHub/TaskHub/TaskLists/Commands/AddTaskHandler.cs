using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Commands;

public partial class AddTaskHandler : ICommandHandler<AddTask, AddTaskHandler.State>
{
    public IStreamName StreamName(AddTask command) => new TaskListStream(command.TaskListId);

    public IEnumerable<IEvent> Handle(AddTask command, State state)
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

    [State]
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

    public IScenario[] Scenarios =>
    [
        new Scenario("task is added")
            .Given()
            .When(new AddTask(Example.Guid, Example.String))
            .Then(new TaskAdded(Example.Guid, Example.String)),
        new Scenario("error when task is already added")
            .Given(new TaskAdded(Example.Guid, Example.String))
            .When(new AddTask(Example.Guid, Example.String))
            .Throws<TaskAlreadyAddedException>(),
        new Scenario("handles user mentions in tasks")
            .Given()
            .When(new AddTask(Example.Guid, "hello @bob"))
            .Then(
                new TaskAdded(Example.Guid, "hello @bob"),
                new UserMentionedInTask(Example.Guid, "hello @bob", "bob")
            )
    ];
}
