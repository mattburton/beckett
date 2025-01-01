using TaskHub.TaskList.Events;

namespace TaskHub.TaskList.GetList;

[ReadModel]
public partial class GetListReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<TaskItem> Tasks { get; init; } = [];

    private void Apply(TaskListAdded e)
    {
        Id = e.Id;
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
}
