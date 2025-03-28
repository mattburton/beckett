namespace Contracts.TaskLists.Queries;

public record GetTaskListQuery(Guid Id) : IQuery<GetTaskListQuery.Result?>
{
    public record Result(Guid Id, string Name, List<TaskItem> Tasks);

    public record TaskItem(string Task, bool Completed);
}
