namespace TaskHub.TaskLists.Slices.GetLists;

public static class GetListsEndpoint
{
    public static async Task<IResult> Handle(IQueryExecutor queryExecutor, CancellationToken cancellationToken)
    {
        var results = await queryExecutor.Execute(new GetListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
