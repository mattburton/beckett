namespace TaskHub.TaskLists.Slices.GetLists;

public static class GetListsEndpoint
{
    public static async Task<IResult> Handle(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var results = await queryDispatcher.Dispatch(new GetListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
