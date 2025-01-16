namespace TaskHub.TaskLists.Slices.TaskLists;

public static class GetTaskListsEndpoint
{
    public static async Task<IResult> Handle(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var results = await queryDispatcher.Dispatch(new TaskListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
