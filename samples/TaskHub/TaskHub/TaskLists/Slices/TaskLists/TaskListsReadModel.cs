using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.TaskLists;

[State]
public partial class TaskListsReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    private void Apply(TaskListAdded message)
    {
        Id = message.Id;
        Name = message.Name;
    }

    private void Apply(TaskListNameChanged message) => Name = message.Name;
}
