using Beckett;
using Taskmaster.TaskLists.GetList;

namespace API.TaskLists;

public static class GetList
{
    public static async Task<IResult> Handler(
        Guid id,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var model = await new GetListQuery(id).Execute(messageStore, cancellationToken);

        return model == null ? Results.NotFound() : Results.Ok(model);
    }
}
