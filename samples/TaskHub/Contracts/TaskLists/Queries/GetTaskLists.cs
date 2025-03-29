namespace Contracts.TaskLists.Queries;

public record GetTaskLists : IQuery<GetTaskLists.Result>
{
    public record Result(IReadOnlyList<TaskList> TaskLists);

    public record TaskList(Guid Id, string Name);
}
