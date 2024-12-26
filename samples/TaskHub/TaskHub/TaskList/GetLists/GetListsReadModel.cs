using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.GetLists;

public class GetListsReadModel : IApply
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public void Apply(object message)
    {
        switch (message)
        {
            case TaskListAdded m:
                Apply(m);
                break;
            case TaskListNameChanged m:
                Apply(m);
                break;
        }
    }

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
