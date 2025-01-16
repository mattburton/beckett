namespace TaskHub.TaskLists.Slices.TaskList;

public static class GetTaskListEndpoint
{
    public static async Task<IResult> Handle(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.Dispatch(new TaskListQuery(id), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
