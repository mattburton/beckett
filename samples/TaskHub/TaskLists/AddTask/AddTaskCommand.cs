using TaskLists.Events;

namespace TaskLists.AddTask;

public partial record AddTaskCommand(Guid TaskListId, string Task) : ICommand<AddTaskCommand.State>
{
    public IStreamName StreamName() => new TaskListStream(TaskListId);

    public IEnumerable<IInternalEvent> Execute(State state)
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

    public IScenario[] Scenarios =>
    [
        new Scenario("task is added")
            .Given()
            .When(new AddTaskCommand(Example.Guid, Example.String))
            .Then(new TaskAdded(Example.Guid, Example.String)),
        new Scenario("error when task is already added")
            .Given(new TaskAdded(Example.Guid, Example.String))
            .When(new AddTaskCommand(Example.Guid, Example.String))
            .Throws<TaskAlreadyAddedException>(),
        new Scenario("handles user mentions in tasks")
            .Given()
            .When(new AddTaskCommand(Example.Guid, "hello @bob"))
            .Then(
                new TaskAdded(Example.Guid, "hello @bob"),
                new UserMentionedInTask(Example.Guid, "hello @bob", "bob")
            )
    ];

    internal static partial class Matcher
    {
        [GeneratedRegex(@"@(\w+)\b")]
        public static partial Regex Username();
    }
}
