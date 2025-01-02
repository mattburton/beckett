namespace TaskHub.TaskLists.GetLists;

public class GetListsHandler
{
    public static async Task<IResult> Get(
        IDatabase database,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new GetListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
