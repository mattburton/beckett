namespace TaskHub.TaskLists.Slices.GetList;

public static class GetListEndpoint
{
    public static async Task<IResult> Handle(Guid id, IQueryExecutor queryExecutor, CancellationToken cancellationToken)
    {
        var result = await queryExecutor.Execute(new GetListQuery(id), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
