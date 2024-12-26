namespace TaskHub.TaskList.GetList;

public class GetListHandler
{
    public static async Task<IResult> Get(
        Guid id,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var model = await new GetListQuery(id).Execute(messageStore, cancellationToken);

        return model == null ? Results.NotFound() : Results.Ok(model);
    }
}
