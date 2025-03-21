namespace TaskHub.TaskLists.Slices.TaskLists;

public static class TaskListsEndpoint
{
    public static async Task<IResult> Handle(IQueryBus queryBus, CancellationToken cancellationToken)
    {
        var results = await queryBus.Send(new TaskListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
