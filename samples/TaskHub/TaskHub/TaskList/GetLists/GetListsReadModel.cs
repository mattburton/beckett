using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.GetLists;

[ReadModel]
public partial class GetListsReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    private void Apply(TaskListAdded message)
    {
        Id = message.Id;
        Name = message.Name;
    }

    private void Apply(TaskListNameChanged message)
    {
        Name = message.Name;
    }
}
