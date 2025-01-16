namespace TaskHub.TaskLists.Slices.GetList;

public static class GetListEndpoint
{
    public static async Task<IResult> Handle(Guid id, IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.Dispatch(new GetListQuery(id), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
