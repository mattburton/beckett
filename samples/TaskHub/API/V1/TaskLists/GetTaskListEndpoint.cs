using Contracts.TaskLists.Queries;

namespace API.V1.TaskLists;

public static class GetTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        var result = await module.Execute(new GetTaskList(taskListId), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
