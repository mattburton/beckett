namespace Contracts.TaskLists.Queries;

public record GetTaskListsQuery : IQuery<GetTaskListsQuery.Result>
{
    public record Result(IReadOnlyList<TaskList> TaskLists);

    public record TaskList(Guid Id, string Name);
}
