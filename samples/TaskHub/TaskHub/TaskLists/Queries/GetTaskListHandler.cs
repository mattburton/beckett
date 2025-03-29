using Contracts.TaskLists.Queries;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Queries;

public partial class GetTaskListHandler(
    IStreamReader reader
) : StreamStateQueryHandler<GetTaskList, GetTaskListHandler.State, GetTaskList.Result?>(reader)
{
    protected override IStreamName StreamName(GetTaskList query) => new TaskListStream(query.Id);

    protected override GetTaskList.Result Map(State model) =>
        new(
            model.Id,
            model.Name,
            model.Tasks.Select(x => new GetTaskList.TaskItem(x.Task, x.Completed)).ToList()
        );

    [State]
    public partial class State : IHaveScenarios
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public List<TaskItem> Tasks { get; private init; } = [];

        private void Apply(TaskListAdded e)
        {
            Id = e.Id;
            Name = e.Name;
        }

        private void Apply(TaskListNameChanged e)
        {
            Name = e.Name;
        }

        private void Apply(TaskAdded e) => Tasks.Add(new TaskItem(e.Task, false));

        private void Apply(TaskCompleted e)
        {
            var current = Tasks.Single(x => x.Task == e.Task);

            var updated = current with { Completed = true };

            Tasks.Replace(current, updated);
        }

        public record TaskItem(string Task, bool Completed);

        public IScenario[] Scenarios =>
        [
            new Scenario("task list added")
                .Given(new TaskListAdded(Example.Guid, Example.String))
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = Example.String
                    }
                ),
            new Scenario("task list name changed")
                .Given(
                    new TaskListAdded(Example.Guid, "original name"),
                    new TaskListNameChanged(Example.Guid, "new name")
                )
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = "new name"
                    }
                ),
            new Scenario("task is included in list")
                .Given(
                    new TaskListAdded(Example.Guid, Example.String),
                    new TaskAdded(Example.Guid, Example.String)
                )
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = Example.String,
                        Tasks =
                        [
                            new TaskItem(Example.String, false)
                        ]
                    }
                ),
            new Scenario("multiple tasks are included in the list")
                .Given(
                    new TaskListAdded(Example.Guid, Example.String),
                    new TaskAdded(Example.Guid, "task-1"),
                    new TaskAdded(Example.Guid, "task-2"),
                    new TaskAdded(Example.Guid, "task-3")
                )
                .Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = Example.String,
                        Tasks =
                        [
                            new TaskItem("task-1", false),
                            new TaskItem("task-2", false),
                            new TaskItem("task-3", false)
                        ]
                    }
                ),
            new Scenario("completed tasks are updated in the list")
                .Given(
                    new TaskListAdded(Example.Guid, Example.String),
                    new TaskAdded(Example.Guid, Example.String),
                    new TaskCompleted(Example.Guid, Example.String)
                ).Then(
                    new State
                    {
                        Id = Example.Guid,
                        Name = Example.String,
                        Tasks =
                        [
                            new TaskItem(Example.String, true)
                        ]
                    }
                )
        ];
    }
}
