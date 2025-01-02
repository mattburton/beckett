namespace TaskHub.TaskLists.GetList;

public static class GetListHandler
{
    public static async Task<IResult> Get(
        Guid id,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(TaskList.StreamName(id), cancellationToken);

        var state = stream.IsEmpty ? null : stream.ProjectTo<TaskListDetails>();

        return state == null ? Results.NotFound() : Results.Ok(state);
    }
}
