namespace Contracts.TaskLists.Queries;

public record GetTaskList(Guid Id) : IQuery<GetTaskList.Result?>
{
    public record Result(Guid Id, string Name, List<TaskItem> Tasks);

    public record TaskItem(string Task, bool Completed);
}
