using TaskLists.Events;

namespace TaskLists.GetLists;

[State]
public partial class GetListsReadModel : IStateView
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    private void Apply(TaskListAdded message)
    {
        Id = message.Id;
        Name = message.Name;
    }

    private void Apply(TaskListNameChanged message) => Name = message.Name;

    public IScenario[] Scenarios =>
    [
        new Scenario("task list added")
            .Given(new TaskListAdded(Example.Guid, Example.String))
            .Then(
                new GetListsReadModel
                {
                    Id = Example.Guid,
                    Name = Example.String
                }
            ),
        new Scenario("task list name changed")
            .Given(
                new TaskListAdded(Example.Guid, "old name"),
                new TaskListNameChanged(Example.Guid, "new name")
            )
            .Then(
                new GetListsReadModel
                {
                    Id = Example.Guid,
                    Name = "new name"
                }
            )
    ];
}
