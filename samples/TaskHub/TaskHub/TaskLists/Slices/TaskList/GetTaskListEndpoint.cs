namespace TaskHub.TaskLists.Slices.TaskList;

public static class GetTaskListEndpoint
{
    public static async Task<IResult> Handle(Guid id, IQueryBus queryBus, CancellationToken cancellationToken)
    {
        var result = await queryBus.Send(new TaskListQuery(id), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
