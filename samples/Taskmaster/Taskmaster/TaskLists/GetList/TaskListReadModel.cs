using Taskmaster.TaskLists.AddTask;
using Taskmaster.TaskLists.CompleteTask;
using Taskmaster.TaskLists.CreateList;

namespace Taskmaster.TaskLists.GetList;

public class TaskListReadModel : IApply
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Dictionary<string, bool> Items { get; set; } = [];

    public void Apply(object message)
    {
        switch (message)
        {
            case TaskListCreated e:
                Apply(e);
                break;
            case TaskAdded e:
                Apply(e);
                break;
            case TaskCompleted e:
                Apply(e);
                break;
        }
    }

    private void Apply(TaskListCreated e)
    {
        Id = e.Id;
        Name = e.Name;
    }

    private void Apply(TaskAdded e) => Items.Add(e.Task, false);

    private void Apply(TaskCompleted e) => Items[e.Item] = true;
}
